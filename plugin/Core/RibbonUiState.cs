using Autodesk.Revit.UI;
using System;

namespace revit_mcp_plugin.Core
{
    internal static class RibbonUiState
    {
        private static PushButton _toggleButton;
        private static TextBox _statusTextBox;

        public static void Initialize(PushButton toggleButton, TextBox statusTextBox)
        {
            _toggleButton = toggleButton;
            _statusTextBox = statusTextBox;

            SetStatus("MCP: Closed", "MCP server is closed");
        }

        public static void SetStatus(string text, string tooltip = null)
        {
            try
            {
                if (_statusTextBox != null)
                {
                    _statusTextBox.Value = text;
                    if (!string.IsNullOrWhiteSpace(tooltip))
                        _statusTextBox.ToolTip = tooltip;
                }

                if (_toggleButton != null && !string.IsNullOrWhiteSpace(tooltip))
                {
                    _toggleButton.ToolTip = tooltip;
                }
            }
            catch (Exception)
            {
                // Ribbon UI updates should never block command execution.
            }
        }
    }
}
