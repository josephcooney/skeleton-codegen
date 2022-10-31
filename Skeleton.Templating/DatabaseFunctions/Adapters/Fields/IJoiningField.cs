using System;
using System.Collections.Generic;
using System.Text;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters.Fields
{
    public interface IJoiningField : IPseudoField
    {
        string PrimaryAlias { get; }
    }
}
