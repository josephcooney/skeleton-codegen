using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.DatabaseFunctions.Adapters;

public class LinkAdapter
{
    public LinkAdapter(ApplicationType linkType, ApplicationType currentType, ApplicationType otherSide)
    {
        LinkType = linkType;
        OtherSideOfLink = otherSide;
        CurrentType = currentType;
    }
    public ApplicationType CurrentType { get; private set; }
    public ApplicationType LinkType { get; private set; }
    public ApplicationType OtherSideOfLink { get; private set; }

    public Field LinkingFieldToOtherSide => LinkType.Fields.First(f =>
        f.HasReferenceType && f.ReferencesType != CurrentType && !f.ReferencesType.IsSecurityPrincipal);
}