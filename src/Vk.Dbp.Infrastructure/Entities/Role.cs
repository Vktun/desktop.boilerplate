using System.ComponentModel.DataAnnotations;

namespace Dabp.Infrastructure.Entities
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
      [StringLength(50)]
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        /// <summary>
        /// 角色级别，默认可以不用
        /// </summary>
        public int RoleLevel { get; set; }
    }
}
