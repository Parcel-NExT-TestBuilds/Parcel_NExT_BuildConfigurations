using Parcel.Neo.Base.Framework;
using Parcel.Neo.Base.Framework.Advanced;
using Parcel.Neo.Base.Framework.ViewModels.BaseNodes;
using Parcel.Neo.Base.Framework.ViewModels.Primitives;
using Parcel.Neo.Base.Toolboxes.Basic.Nodes;

namespace Parcel.Neo.Base.Toolboxes.Basic
{
    public class BasicToolbox : IToolboxDefinition
    {
        #region Interface
        public ToolboxNodeExport?[] ExportNodes => new ToolboxNodeExport?[]
        {
            new("Comment", typeof(CommentNode))
            {
                Tooltip = "A node that allows grouping other nodes together and provide descriptive comment."
            },
            new("Select Output", typeof(SelectOutputNode))
            {
                Tooltip = "Select output from connected pin."
            },
            new("Preview", typeof(PreviewNode))
            {
                Tooltip = "Preview results."
            },
            null, // Divisor line // Primitive Nodes
            new("Number", typeof(NumberNode))
            {
                Tooltip = "Defines a literal number."
            },
            new("String", typeof(StringNode))
            {
                Tooltip = "Defines a literal string."
            },
            new("Boolean", typeof(BooleanNode))
            {
                Tooltip = "Defines a literal boolean."
            },
            new("DateTime", typeof(DateTimeNode))
            {
                Tooltip = "Defines a literal date time."
            },
            new("Password", typeof(PasswordNode))
            {
                Tooltip = "Defines a literal string that is shown as password."
            },
            new("Text", typeof(TextNode))
            {
                Tooltip = "Defines a literal string that covers multiple spans."
            },
            new("File", typeof(OpenFileNode))
            {
                Tooltip = "Defines a literal string that points to a file."
            },
            // new("Save File", typeof(object)),
            // new("Array", typeof(object)), // Generic array representation of all above types, CANNOT have mixed types
            null, // Divisor line // Advanced Types
            new("Data Table", typeof(DataTable))
            {
                Tooltip = "Defines an empty data table; Use this to initialize dataTable or matrix."
            },
            new("Dictionary", typeof(Dictionary))
            {
                Tooltip = "Defines a dictionary mapping of keys to values."
            },
            null, // Divisor line // Graph Modularization
            new("Graph Input", typeof(GraphInput))
            {
                Tooltip = "Defines inputs to this graph."
            },
            new("Graph Output", typeof(GraphOutput))
            {
                Tooltip = "Defines outputs of this graph."
            },
            new("Graph Reference", typeof(GraphReferenceNode))
            {
                Tooltip = "Defines reference to an existing graph."
            },
            // new("Sub Graph", typeof(object)),
            null, // Divisor Line // Special (or consider moving them into "Annotation")
            // Special - Specialized Graph Visualization
            new("Graph Stats", typeof(GraphStats))
            {
                Tooltip = "A on-canvas display of current graph stats."
            },
            //new("Console Output", typeof(object)), // With options to specify how many lines to show
            //new("Python Snippet", typeof(object)), // With auto binding inputs and outputs
            //null, // Divisor line // Utility
            //new("Graph Attributes", typeof(object)),
            //null, // Divisor line // Decoration
            //new("Header", typeof(object)),
            //new("Text", typeof(object)),
            //new("URL", typeof(object)),
            //new("Image", typeof(object)),
            //new("Markdown", typeof(object)),
            //new("Audio", typeof(object)),
            //new("Web Page", typeof(object)),
            //new("Help Page", typeof(object)),
            //null, // Divisor line // Others
            //new("Contact", typeof(object)),
            //new("About", typeof(object)),
            
            // Control Flow
            //new ToolboxNodeExport("State Graph", typeof(object)),    // UE4 Blueprint like execution graph with explicit execution order and support for loops and states
            //null, // Divisor line
            //new ToolboxNodeExport("Action", typeof(object)),    // Like lambda, applied on each element in an array
            //new ToolboxNodeExport("Function", typeof(object)),  // A special node that actually refers to a graph with input and output markers; This graph MUST be stored directly within current graph to avoid unnecessary modularization (aka. we must implement supporting GUI)
            //null, // Divisor line
            //new ToolboxNodeExport("Apply", typeof(object)) // Like Map in JS or Select in C#, takes an Array and a Function node; equivalent as ForEach loop
            // Functional Logic
            // new ("Choose") // Choose based on input, like how it works in Houdini
        };
        #endregion
    }
}