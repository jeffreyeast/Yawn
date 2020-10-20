# Yawn - Yet-Another-Window/Pane manager for WPF applications

Yawn is a class library for use in applications built using Microsoft's Windows Presentation Foundation (WPF). It supports applications which use multiple panes to organize and present information. It allows users to manipulate panes by altering their size and
location, as well as to organize related panes into collections, which in turn can be pinned, unpinned or floated into separate windows. The user interaction model is similar
(but not identical) to that used by Microsoft's Visual Studio products.

## Concepts

Yawn refers to top-level UI elements as "Dockable Content". Examples of dockable content are pages and data entry forms. Dockable content is organized by ItemControls
called DockableCollections. Each DockableCollection belongs to exactly one Dock, which is an ItemsControl which the developer places into their control hierarchy (either with XAML or 
programmatically). 

Docks can be fixed or floating. A floating Dock is owned by a DockWindow, which in turn is owned by the main window. 

Each DockableCollection organizes its DockableContent.  A DockableCollection is either collapsed or not collapsed. A collapsed DockableCollection is not visible on the screen, but the tabs for its
DockableContent are visible at the edges of the Dock. A non-collapsed DockableCollection show the contents of exactly one of its DockableContents. The tabs for all its DockableContents are displayed at either the top
or the bottom of the DockableCollection. The user clicks
on a tab to make its DockableContent visible. (This can be done programmatically using the DockableCollection.Show method.) Each DockableContent can be pinned or unpinned. Pinning content ensures the collection 
will remain visible and won't collapse. A DockableCollection whose content is unpinned will collapse when the focus moves from the content. The tabs for the DockableCollection's DockableContent
will appear along the edge of the dock. Clicking on one of these will cause the collection to expand so the user can view the content. The collection will
collapse again when the focus moves away from the content, unless the user clicks on the push-pin to pin the collection. 

A DockableCollection becomes floating when the user clicks on the "float" command in its context menu. A new DockWindow is created, holding it's own Dock. The DockableCollection
and its DockableContent move to this Dock. 

DockableContent can move from one DockableCollection to another. This is done programmatically by using Add and Remove methods of the DockableCollection.Items collection. The user can drag/drop 
tabs to move content between DockableCollections.

## How to Use Yawn

Look at MainWindow.xaml for an example of a sample application. Note the merged resource dictionary in the <Window.Resources> section -- this, together with the xmlns definition
above it, gives the developer access to the Yawn controls. 

Notice the root control managed by Yawn is the Dock ItemsControl. This ItemsControl children are DockableCollection ItemsControls. Each DockableCollection ItemsControl in turn owns content
controls. The content in the sample program surrounds each piece of content with a Grid panel. This provides a convenient location to use the attached dependency properties
which identify DockableContent name tabs (Yawn.DockingPanel.TabText) and provide context used when saving and restoring pane layouts (Yawn.DockingPanel.DescriptiveText).
It also acknowledges that DockableContent is required to be variable-sized, and provides an easy way to associate
background brushes and such with the content.

The Dock control has no new dependency properties for the developer. It has some new properties used internally to the implementation.

The ItemsPanel for the dock is a new panel called DockingPanel. DockingPanel introduces some new attached dependency properties which developers should become familiar with.

1. DockingPanel.DockPosition (Top/Bottom/Left/Right) is an optional attached dependency properties which instructs how a DockableCollection should be positioned when it is first 
added to the Dock.Items collection. 
2. DockingPanel.DescriptiveText is a string attached dependency property which is provided to enable content to be created (or attached) when a layout is restored.
3. DockingPanel.IsPinned is a boolean attached dependency property which describes if content is pinned (true) or unpinned (false).
4. DockingPanel.TabText is a string attached property which provides the text to be displayed on the content's tab

DockableCollections have several new dependency properties:
1. Description is a string property which is preserved by layout save/restore for use by the application.
2. TabPosition (Top/Buttom) is a property which specifies if the contents' tabs should be positioned at the top of the collection, or the bottom (below the content).

## Saving/Restoring Layouts

The Yawn.Layout class is used to save and restore the layout of a set of Docks. Its Save method creates a snapshot of the layout, and Restore creates a layout from a previously-saved snapshot. The snapshot
can be serialized using binary, XML or JSON serialization. Look at the comments in Yawn/Layout/Layout.cs and the MyContentCreator in the sample program's MainWindow.xaml.cs. The DockableContent within the layout
is created by invoking a DockableContextContentCreator callback for each DockableContent in the layout.

## How to Build Yawn

Yawn is a .Net Framework 4.8 class library, built using Microsoft Visual Studio 2019. Simply download the files, and tell Visual Studio to rebuild.

## Quality, Bugs, Support

Pane management turns out to be a surprisingly difficult problem to solve. There are lots of policy choices, and ones the code makes may, or may not, suit your application. Post issues either at GitHub or CodeProject.
Suggested bug fixes and enhancements should be posted only on GitHub. There is no ongoing commitment for support or bug fixes. See the CPOL license for details.

## Why Yawn? Where is Yawn Going?

Yawn was developed as an open source, free alternative to more sophisticated window/pane managers for WPF applications.
It's evolution depends on who uses it, their needs, and the interest that develops in enhancing it.

## License

Code Project Open License (CPOL) 1.02

## Key Contributors

- Jeff East
