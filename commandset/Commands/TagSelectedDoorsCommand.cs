using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands
{
    public class TagSelectedDoorsCommand : ExternalEventCommandBase
    {
        private TagSelectedDoorsEventHandler _handler => (TagSelectedDoorsEventHandler)Handler;

        public override string CommandName => "tag_selected_doors";

        public TagSelectedDoorsCommand(UIApplication uiApp)
            : base(new TagSelectedDoorsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                bool useLeader = parameters["useLeader"]?.ToObject<bool>() ?? false;
                string tagTypeId = parameters["tagTypeId"]?.ToString();
                List<int> doorIds = parameters["doorIds"]?.ToObject<List<int>>();

                _handler.SetParameters(useLeader, tagTypeId, doorIds);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.TaggingResults;
                }

                throw new TimeoutException("Tag selected doors operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to tag selected doors: {ex.Message}");
            }
        }
    }
}
