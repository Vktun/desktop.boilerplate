using System.Text;
using System.Text.Json;
using SqlSugar;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Interfaces;
using AuditLogEntity = Dabp.Infrastructure.Entities.AuditLog;

namespace Vk.Dbp.AccountModule.Services;

public sealed class DbAuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ISqlSugarClient _db;

    public DbAuditLogService(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<List<AuditLog>> GetAllLogsAsync()
    {
        List<AuditLogEntity> entities = await _db.Queryable<AuditLogEntity>()
            .OrderByDescending(x => x.ExecutionTime)
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<AuditLog?> GetLogByIdAsync(int id)
    {
        AuditLogEntity? entity = await _db.Queryable<AuditLogEntity>()
            .FirstAsync(x => x.Id == id);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<List<AuditLog>> GetLogsByUserIdAsync(int userId)
    {
        List<AuditLogEntity> entities = await _db.Queryable<AuditLogEntity>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ExecutionTime)
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<List<AuditLog>> GetLogsByActionTypeAsync(AuditActionType actionType)
    {
        string actionTypeName = actionType.ToString();
        List<AuditLogEntity> entities = await _db.Queryable<AuditLogEntity>()
            .Where(x => x.ServiceName == actionTypeName)
            .OrderByDescending(x => x.ExecutionTime)
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        List<AuditLogEntity> entities = await _db.Queryable<AuditLogEntity>()
            .Where(x => x.ExecutionTime >= startDate && x.ExecutionTime <= endDate)
            .OrderByDescending(x => x.ExecutionTime)
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<List<AuditLog>> GetLogsByModuleAsync(string module)
    {
        List<AuditLogEntity> entities = await _db.Queryable<AuditLogEntity>()
            .Where(x => x.ModuleName == module)
            .OrderByDescending(x => x.ExecutionTime)
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<bool> CreateAuditLogAsync(AuditLog log)
    {
        AuditLogEntity entity = MapToEntity(log);
        return await _db.Insertable(entity).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> DeleteOldLogsAsync(int retentionDays)
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-retentionDays);
        return await _db.Deleteable<AuditLogEntity>()
            .Where(x => x.ExecutionTime < cutoffDate)
            .ExecuteCommandAsync() >= 0;
    }

    public async Task<bool> LogOperationAsync(
        int userId,
        string username,
        AuditActionType actionType,
        string module,
        string description,
        string? entityType = null,
        int? entityId = null,
        string? oldData = null,
        string? newData = null,
        string? clientIp = null)
    {
        long startedAt = Environment.TickCount64;
        AuditLog log = new()
        {
            UserId = userId,
            Username = string.IsNullOrWhiteSpace(username) ? "system" : username,
            ActionType = actionType,
            Module = module,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            OldData = oldData,
            NewData = newData,
            ClientIp = clientIp,
            OperationTime = DateTime.Now,
            IsSuccess = true
        };

        log.ExecutionTime = Math.Max(0, Environment.TickCount64 - startedAt);
        return await CreateAuditLogAsync(log);
    }

    public async Task<bool> LogFailureAsync(
        int userId,
        string username,
        AuditActionType actionType,
        string module,
        string description,
        string failureReason,
        string? entityType = null,
        int? entityId = null,
        string? clientIp = null)
    {
        long startedAt = Environment.TickCount64;
        AuditLog log = new()
        {
            UserId = userId,
            Username = string.IsNullOrWhiteSpace(username) ? "system" : username,
            ActionType = actionType,
            Module = module,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            FailureReason = failureReason,
            ClientIp = clientIp,
            OperationTime = DateTime.Now,
            IsSuccess = false
        };

        log.ExecutionTime = Math.Max(0, Environment.TickCount64 - startedAt);
        return await CreateAuditLogAsync(log);
    }

    public async Task<byte[]> ExportLogsAsync(List<int> logIds)
    {
        List<long> auditLogIds = logIds.Select(static id => (long)id).ToList();
        List<AuditLogEntity> entities = await _db.Queryable<AuditLogEntity>()
            .Where(x => auditLogIds.Contains(x.Id))
            .OrderByDescending(x => x.ExecutionTime)
            .ToListAsync();

        string json = JsonSerializer.Serialize(entities.Select(MapToModel).ToList(), JsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    public async Task<bool> ClearAllLogsAsync()
    {
        return await _db.Deleteable<AuditLogEntity>().ExecuteCommandAsync() >= 0;
    }

    private static AuditLogEntity MapToEntity(AuditLog log)
    {
        AuditPayload payload = new()
        {
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldData = log.OldData,
            NewData = log.NewData,
            ClientIp = log.ClientIp
        };

        return new AuditLogEntity
        {
            ModuleName = log.Module,
            ServiceName = log.ActionType.ToString(),
            MethodName = log.Description,
            IsSuccess = log.IsSuccess,
            Parameters = JsonSerializer.Serialize(payload, JsonOptions),
            UserId = log.UserId,
            UserName = log.Username,
            ExecutionTime = log.OperationTime,
            ExecutionDuration = log.ExecutionTime,
            Exceptions = log.FailureReason
        };
    }

    private static AuditLog MapToModel(AuditLogEntity entity)
    {
        AuditPayload? payload = null;
        if (!string.IsNullOrWhiteSpace(entity.Parameters))
        {
            try
            {
                payload = JsonSerializer.Deserialize<AuditPayload>(entity.Parameters, JsonOptions);
            }
            catch
            {
                payload = null;
            }
        }

        return new AuditLog
        {
            Id = (int)entity.Id,
            UserId = entity.UserId,
            Username = entity.UserName,
            ActionType = Enum.TryParse(entity.ServiceName, out AuditActionType actionType)
                ? actionType
                : AuditActionType.View,
            Module = entity.ModuleName,
            Description = entity.MethodName,
            EntityType = payload?.EntityType,
            EntityId = payload?.EntityId,
            OldData = payload?.OldData,
            NewData = payload?.NewData,
            ClientIp = payload?.ClientIp,
            OperationTime = entity.ExecutionTime,
            ExecutionTime = entity.ExecutionDuration,
            IsSuccess = entity.IsSuccess,
            FailureReason = entity.Exceptions
        };
    }

    private sealed class AuditPayload
    {
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        public string? OldData { get; set; }

        public string? NewData { get; set; }

        public string? ClientIp { get; set; }
    }
}
