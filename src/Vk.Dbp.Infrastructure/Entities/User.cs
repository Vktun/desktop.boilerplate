using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dabp.Infrastructure.Entities
{
    public class User
    {
        [Key]

        public int Id { get; set; }
        /// <summary>
        /// 登录名//用户名
        /// </summary>
    [StringLength(50)]
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
    [StringLength(100)]

        public string PasswordHash { get; set; }
        /// <summary>
        /// 昵称
        /// </summary>
              [StringLength(50)]
        public string SurName { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        [StringLength(11)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// 最后一次修改密码时间
        /// </summary>
        public DateTime ChangePasswordLastTime { get; set; }
        /// <summary>
        /// 用户密码过期时间
        /// </summary>
        public int ValideDays { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// 创建人ID
        /// </summary>
        public int CreatorId { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        [AllowNull]
        public DateTime? LastModificationTime { get; set; }
        /// <summary>
        /// 最后修改人ID
        /// </summary>
        [AllowNull]
        public int? LastModifierId { get; set; }
        /// <summary>
        /// 删除时间
        /// </summary>
        [AllowNull]
        public DateTime? DeletionTime { get; set; }
        /// <summary>
        /// 删除人ID
        /// </summary>
        [AllowNull]
        public int? DeleterId { get; set; }
        /// <summary>
        /// 是否删除
        /// </summary>
        public bool IsDeleted { get; set; }

    }
}
