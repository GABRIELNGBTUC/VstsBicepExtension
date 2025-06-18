using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TucRail.Bicep.VsMarketplace.Shared;

public class EnumHelper
{
    /// <summary>
    /// Retrieve the name property from the DisplayAttribute
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string? GetDisplayName(Enum? value)
    {
        if (value == null)
        {
            return null;
        }
        
        // Obtenir le type d'énumération
        Type enumType = value.GetType();

        // Récupérer le membre correspondant à l'énumération
        MemberInfo? memberInfo = enumType.GetMember(value.ToString()).FirstOrDefault();

        if (memberInfo != null)
        {
            // Vérifier si un DisplayAttribute est défini sur ce membre
            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();

            // Retourner la propriété Name ou null si aucun DisplayAttribute n'est présent
            return displayAttribute?.Name;
        }

        return null;
    }
}