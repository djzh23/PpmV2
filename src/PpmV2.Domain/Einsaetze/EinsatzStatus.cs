using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Domain.Einsaetze;

public enum EinsatzStatus
{
    Draft = 0,
    Planned = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}
