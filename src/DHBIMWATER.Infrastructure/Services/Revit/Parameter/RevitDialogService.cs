using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class RevitDialogService : IDialogService
    {
        public void Info(string title, string message) => TaskDialog.Show(title, message);

        public void Warn(string title, string message)
        {
            TaskDialog taskDialog = new TaskDialog(title)
            {
                MainInstruction = message,
                CommonButtons = TaskDialogCommonButtons.Ok,
                MainIcon = TaskDialogIcon.TaskDialogIconWarning
            };

            taskDialog.Show();
        }

        public bool Confirm(string title, string message)
        {
            TaskDialog taskDialog = new TaskDialog(title)
            {
                MainInstruction = message,
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
            };

            return taskDialog.Show() == TaskDialogResult.Yes;
        }


        
    }
}
