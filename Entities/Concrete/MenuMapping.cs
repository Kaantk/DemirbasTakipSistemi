using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace Entities.Concrete
{
    public class MenuMapping : IEntity
    {
        public int Id { get; set; }
        public int? MainMenuId { get; set; }
        public int? SubMenuId { get; set; }
    }
}
