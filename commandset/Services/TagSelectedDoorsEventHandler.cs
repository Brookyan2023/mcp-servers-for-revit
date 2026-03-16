using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class TagSelectedDoorsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        private bool _useLeader;
        private string _tagTypeId;
        private List<int> _doorIds;

        public object TaggingResults { get; private set; }

        public void SetParameters(bool useLeader, string tagTypeId, List<int> doorIds = null)
        {
            _useLeader = useLeader;
            _tagTypeId = tagTypeId;
            _doorIds = doorIds;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiApp)
        {
            _uiApp = uiApp;

            try
            {
                View activeView = _doc.ActiveView;
                if (activeView == null || activeView.IsTemplate)
                {
                    TaggingResults = new
                    {
                        success = false,
                        message = "Active view does not support door tagging."
                    };
                    return;
                }

                ICollection<Element> doors = GetTargetDoors(activeView);
                if (doors.Count == 0)
                {
                    TaggingResults = new
                    {
                        success = false,
                        message = "No selected doors found in the active view."
                    };
                    return;
                }

                List<object> createdTags = new List<object>();
                List<string> errors = new List<string>();
                FamilySymbol doorTagType = FindDoorTagType(_doc);

                if (doorTagType == null)
                {
                    TaggingResults = new
                    {
                        success = false,
                        message = "No door tag family type found in the project."
                    };
                    return;
                }

                using (Transaction transaction = new Transaction(_doc, "Tag Selected Doors"))
                {
                    transaction.Start();

                    if (!doorTagType.IsActive)
                    {
                        doorTagType.Activate();
                        _doc.Regenerate();
                    }

                    foreach (Element door in doors)
                    {
                        try
                        {
                            XYZ tagPoint = GetTagPoint(door, activeView);
                            if (tagPoint == null)
                            {
                                errors.Add($"Could not determine tag location for door {ElementIdToLong(door.Id)}.");
                                continue;
                            }

                            IndependentTag tag = IndependentTag.Create(
                                _doc,
                                doorTagType.Id,
                                activeView.Id,
                                new Reference(door),
                                _useLeader,
                                TagOrientation.Horizontal,
                                tagPoint);

                            if (tag != null)
                            {
                                createdTags.Add(new
                                {
                                    tagId = ElementIdToLong(tag.Id).ToString(),
                                    doorId = ElementIdToLong(door.Id).ToString(),
                                    doorName = door.Name,
                                    location = new
                                    {
                                        x = tagPoint.X * 304.8,
                                        y = tagPoint.Y * 304.8,
                                        z = tagPoint.Z * 304.8
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error tagging door {ElementIdToLong(door.Id)}: {ex.Message}");
                        }
                    }

                    transaction.Commit();
                }

                TaggingResults = new
                {
                    success = true,
                    selectedDoors = doors.Count,
                    taggedDoors = createdTags.Count,
                    tags = createdTags,
                    errors = errors.Count > 0 ? errors : null,
                    message = $"Created {createdTags.Count} door tags from {doors.Count} selected doors."
                };
            }
            catch (Exception ex)
            {
                TaggingResults = new
                {
                    success = false,
                    message = $"Error occurred: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Tag Selected Doors";
        }

        private ICollection<Element> GetTargetDoors(View activeView)
        {
            IEnumerable<ElementId> sourceIds = _doorIds != null && _doorIds.Count > 0
                ? _doorIds.Select(id => new ElementId(id))
                : _uiDoc.Selection.GetElementIds();

            return sourceIds
                .Select(id => _doc.GetElement(id))
                .Where(element =>
                    element != null &&
                    element.Category != null &&
                    GetElementIdIntValue(element.Category.Id) == (int)BuiltInCategory.OST_Doors &&
                    (element.OwnerViewId == ElementId.InvalidElementId || element.OwnerViewId == activeView.Id))
                .ToList();
        }

        private XYZ GetTagPoint(Element door, View activeView)
        {
            if (door.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point;
            }

            BoundingBoxXYZ boundingBox = door.get_BoundingBox(activeView) ?? door.get_BoundingBox(null);
            if (boundingBox == null)
            {
                return null;
            }

            return (boundingBox.Min + boundingBox.Max) / 2.0;
        }

        private FamilySymbol FindDoorTagType(Document doc)
        {
            if (!string.IsNullOrEmpty(_tagTypeId) && int.TryParse(_tagTypeId, out int parsedId))
            {
                Element element = doc.GetElement(new ElementId(parsedId));
                if (element is FamilySymbol symbol &&
                    symbol.Category != null &&
                    (GetElementIdIntValue(symbol.Category.Id) == (int)BuiltInCategory.OST_DoorTags ||
                     GetElementIdIntValue(symbol.Category.Id) == (int)BuiltInCategory.OST_MultiCategoryTags))
                {
                    return symbol;
                }
            }

            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .FirstOrDefault(symbol =>
                    symbol.Category != null &&
                    (GetElementIdIntValue(symbol.Category.Id) == (int)BuiltInCategory.OST_DoorTags ||
                     GetElementIdIntValue(symbol.Category.Id) == (int)BuiltInCategory.OST_MultiCategoryTags));
        }

        private static long ElementIdToLong(ElementId id)
        {
#if REVIT2024_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }

        private static int GetElementIdIntValue(ElementId id)
        {
#if REVIT2024_OR_GREATER
            return unchecked((int)id.Value);
#else
            return id.IntegerValue;
#endif
        }
    }
}
