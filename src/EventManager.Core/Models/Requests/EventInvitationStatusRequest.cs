using System.ComponentModel.DataAnnotations;

namespace EventManager.Core.Models.Requests;

public class EventInvitationStatusRequest
{
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("accept|decline")]
    public string Status { get; set; }
}