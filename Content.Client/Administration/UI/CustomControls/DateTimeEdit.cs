using System.Text;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.CustomControls
{
    /// <summary>
    ///     Allows the user to input and modify a line of date time.
    /// </summary>
    [Virtual]
    public class DateTimeEdit : LineEdit
    {
        public DateTime? TryGetDateTime()
        {
            if (!DateTime.TryParse(this.Text, out var value))
            {
                return null;
            }

            return value;
        }
    }
}
