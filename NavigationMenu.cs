using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakNSave
{
    class NavigationMenu
    {
    }

    public class MenuData
    {
        public string URL { get; set; }
        public string Name { get; set; }
        public string ItemName { get; set; }
        public List<object> Children { get; set; }
        public bool IsInMobileHeader { get; set; }
        public string ItemId { get; set; }
    }

    public class NavigationList
    {
        public string URL { get; set; }
        public string Name { get; set; }
        public string ItemName { get; set; }
        public List<MenuData> Children { get; set; }
        public bool IsInMobileHeader { get; set; }
        public string ItemId { get; set; }
        public string LinkCSS { get; set; }
    }

    public class NavigationData
    {
        public List<NavigationList> NavigationList { get; set; }
        public bool Success { get; set; }
    }
}
