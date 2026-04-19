using System.ComponentModel.DataAnnotations;

using SqlSugar;

namespace Dabp.Infrastructure.Entities
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        [SugarColumn(ColumnDataType = "nvarchar")]
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        /// <summary>
        /// 角色级别，默认可以不用
        /// </summary>
        public int RoleLevel { get; set; }
    }
}
