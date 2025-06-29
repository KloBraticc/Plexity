﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plexity.Models.Attributes
{
    class EnumNameAttribute : Attribute
    {
        public string? StaticName { get; set; }
        public string? FromTranslation { get; set; }
    }
}
