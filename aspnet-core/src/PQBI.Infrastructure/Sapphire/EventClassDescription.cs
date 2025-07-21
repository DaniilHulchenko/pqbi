using PQS.Data.Events.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.Infrastructure.Sapphire;

public class EventClassDescription
{
    public EventClass EventClass { get; set; }
    public string Alias { get; set; }
    public string Description { get; set; }

}

