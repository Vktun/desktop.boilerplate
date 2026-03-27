using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vk.Dbp.AccountModule.Models
{
    public class OrganizationUnitModel
    {
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public string Code { get; set; }

        public int ParentId { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? LastModificationTime { get; set; }

        public ObservableCollection<OrganizationUnitModel> Children { get; set; } = new ObservableCollection<OrganizationUnitModel>();

        public List<int> MemberUserIds { get; set; } = new List<int>();

        public bool IsExpanded { get; set; }

        public bool IsSelected { get; set; }
    }
}
