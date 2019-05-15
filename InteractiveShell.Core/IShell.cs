
namespace InteractiveShell.Core
{
    public interface IShell
    {
        void ExecuteCommand(string command, int timeoutSeconds = 30);
    }
}
