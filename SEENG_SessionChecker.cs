using Sandbox.ModAPI;

namespace SEENG_ES
{
    public class SessionChecker
    {


        public bool HasSEENGTag(IMyCockpit cockpit)
        {
            return cockpit != null && (cockpit.DisplayNameText ?? "").Contains("[SEENG]");
        }

        public bool IsLocalPlayerIn(IMyCockpit cockpit)
        {
            return cockpit != null && cockpit.IsUnderControl;
        }
    }
}