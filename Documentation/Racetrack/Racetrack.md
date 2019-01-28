Overview
===============================================================================

This racetrack building system allows you to build racetracks by warping meshes around a series of curves. It is particularly suited to building elevated, rollercoaster-like tracks. It features the ability to create jumps, banked corners and even loops.

![Race track with terrain](images/track.jpg)

The resulting mesh is fully drivable, for example using the Unity Standard Assets car, and contains runtime information for tracking a vehicle's progress.

The package contains a "progress tracker" component that can be attached to a car to track its progress around the race track, detect when it has fallen off and automatically place it back on track after a short delay.

Example scene
===============================================================================

See the Assets/Racetrack Builder/Example.unity scene for a simple example racetrack.

![Example race track](images/exampletrack.jpg)

Driving on the track
--------------------

You can drive on the track using the Unity Standard Assets car.

Search the Asset Store for the "standard assets" package. Import everything in the "Standard Assets/Vehicles/Car" sub-folder of the package.

To setup the car:
1. Drag the "Car" prefab from "Assets/Standard Assets/Vehicles/Car/Prefabs" into the scene.
2. Drag the "Main Camera" in the scene tree and make it a child object of the Car.
3. Select the "Main Camera". Reset its transform in the Inspector window.
4. Move the camera behind and above the car a little (e.g. Y=2.3, Z=-6)
5. Drag the "RacetrackProgressTracker" script from "Assets/Racetrack builder/Scripts/Runtime" onto the car.
6. Select the car and find the "Racetrack Progress Tracker (Script)" component in the Inspector window, and click "Reset car".
7. Run the scene and drive with the arrow keys. If you fall off the track, wait 10 seconds and you will respawn back onto it.

Quick start - Creating a track
===============================================================================

It only takes a few minutes to get started building a race track:

First create a new scene (File > New Scene).

You may want to add a plane for the ground (Game Object > 3D Object Plane), reset its position and scale it by (say) 500.

Create the initial track as follows:

1. Create the track by selecting: "GameObject > 3D Object > Racetrack" from the menu. Move it slightly above the ground plane.
2. Expand the new Racetrack object. Select the single "Curve" child object.
3. Navigate to the "Assets/Racetrack Builder/Prefabs/Track Templates" project folder.
4. With the Racetrack Curve still selected, drag the "asphalt poles" prefab and drop it on the "Template" field in the Racetrack Curve properties.
5. Click the "Curve" button next to "Update" in the Racetrack Curve properties

You should see a small piece of road supported on poles.

![Track piece](images/trackpiece.jpg)

Notice the white line and arrow. This traces the path of the curve. The track is created by copying the "template" defined by the prefab and fitting it to the curve.

The curve property editor has a number of buttons for setting the length, turn, gradient and bank angles.

![Curve properties](images/curveproperties.jpg)

Try clicking them to see how the track adapts to the updated curve.

For finer control you can set the values via the slider or key them in. You will need to explicitly rebuild the mesh model by clicking the "Update" button in this case.

To build onto the track, click the "Add curve" button. This creates a new curve with the same properties as the current one, and selects it. 

Using the "Add curve" and Racetrack Curve properties UI, you can quickly build out interesting elevated race tracks.

Track building
===============================================================================

Selecting corners
-----------------

If you need to go back and change a previous corner, there are a couple of ways to do this.

You can expand the "Racetrack" object in the scene tree and click on the corresponding "Curve" child object.

Alternatively, you can click on the track in the editor view. This selects the mesh that was generated from the curve, which is placed underneath the curve in the scene tree.

![Curve meshes](images/curvemeshes.jpg)

Look back up the scene tree to find the corresonding Curve and select it.

Changing a previous curve has a flow on affect that affects where the rest of the curves in the track are positioned.

Be aware that changing a curve's length can also be quite slow, as all the meshes for the remaining curves in the track must be regenerated so that the meshes line up correctly.

Changing a curve's angles is faster, because only the curve and its immediate neighbours need to be rebuilt. (The remaining curves are simply repositioned, which is faster.)

Changing the track type
-----------------------

You can change the type of track by dragging a different prefab from the "Assets/Racetrack builder/Prefabs/Track Templates" project folder and dropping it on the "Template" field in the properties of the corresponding curve.

Then click "Rest of track" in the curve properties, to update the remainder of the track.

![Track type change](images/tracktypechange.jpg)

This will set the track type from this curve onward, or until it is changed again in a later curve.

Creating jumps
--------------

To create a jump, select a curve and tick "Is Jump" property. Then click "Rest of track" to update the rest of the track.

No meshes will be generated for the curve, resulting in a gap which the player must jump over.

![Jump](images/jump.jpg)

Respawning
----------

The RacetrackProgressTracker component can be added to the player car to automatically respawn them back on the track after they fall off. By default the player is respawned on the last curve that they drove on.

Sometimes a curve is not suitable as a respawn point, e.g. if the player cannot get enough speed to complete the next jump, and therefore cannot progress.

To avoid this, untick the "Can Respawn" property of the curve. The respawn logic will search backwards from the last curve the player drove on until it finds a curve where "Can Respawn" is ticked.

Creating mesh templates
===============================================================================

A "mesh template" provides the meshes that are generated and fitted to the path of the curves, in order to create the racetrack.

They are a little bit like Unity prefabs, and are often stored as one. But they are instantiated a little differently, mainly due to the need to warp the meshes along the curves and clone the repeating parts (like support poles).

This package contains a set of mesh templates to get you started, but you can easily build your own to create your own road types.

Supplied templates
------------------

The sample mesh templates are in: Assets/Racetrack builder/Prefabs/Track Templates

There are also prefabs that can be composed together to create mesh templates in: Assets/Racetrack builder/Prefabs/Template parts

These are grouped into folders:
* Driving surface - The main part the player drives on
* Sides - Barriers etc that attach to the side of the track
* Supports - Support poles that hold up the track

Example walkthrough
-------------------

As an example, we can can create a mesh template consisting of wooden planks, support poles and side barriers.

First navigate to the "Assets/Racetrack builder/Prefabs/Track Templates" project folder and drag the "template base" prefab into your scene. This creates the basic skeleton structure for a mesh template. You can examine the object in the scene tree:

* The main object ("template base") contains a **Racetrack Mesh Template** script component. This marks it as a mesh template and is mandatory.

* Underneath we have a "continuous" object with a **Racetrack Continuous** script. This denotes the meshes underneath it as "continuous", meaning they will be warped along the racetrack curves. In this prefab the driving surface and barrier meshes will be "continuous", so that they follow the racetrack curves.

* We also have a "spaced" object, which is an empty placeholder for now. This will be used for objects that will be placed along the track at regular intervals, like the supporting poles.

Create the road surface as follows:
1. Make sure the "template base" object is expanded in the scene tree, and the "continuous" child object is visible.
2. Open the "Assets/Racetrack builder/Prefabs/Template parts/Driving surface" project folder.
3. Drag the "woodplanks" prefab into the *scene tree* and drop it onto the "continuous" child object.
4. Reset the "Position" of the new object to (0,0,0) in the Inspector window.

If you view the template object in the scene, you'll see it now has a woodplanks surface. This is now a fully functional mesh template, which you can drag from the scene tree and drop onto the "Template" property of a racetrack curve. But we are not finished yet.

![New mesh template](images/newtemplate.jpg)

Placing the woodplanks mesh underneath the "continuous" object means it will be warped along the racetrack curves. The first continuous mesh is also special, as it will be treated as the main driving surface. This has certain implications:
* It defines the length of the mesh template.
* It means a "Racetrack Surface" script component will be attached to the mesh after it has been copied and warped. This is used by the RacetrackProgressTracker component to detect when the player is above the road. It also links back to the curve that generated it, so that the player's progress along the track can be calculated.

You may notice the template looks a little small. This is because the default mesh templates are all scaled by a factor of 3. Correct this as follows:
1. Click on the "template base" object in the scene tree.
2. Set the X, Y and Z components of the "Scale" to 3 in the Inspector window.

Now add some poles to hold up the track:
1. Make sure "template base" is still expanded in the scene tree, and that "spaced" is visible.
2. Open the "Assets/Racetrack builder/Prefabs/Template parts/Supports" project folder.
3. Drag the "Metal poles" prefab into the *scene tree* and drop it on the "spaced" child object.
4. Set the Y position to -0.33 in the Inspector window.

The mesh template now has two poles underneath the track. Once again it is fully functional. If you assign it to a curve and regenerate the track, the poles will be spaced along it at regular intervals.

![New mesh template with poles](images/newtemplatepoles.jpg)

Expand the new "Metal poles" object in the scene tree and examine it.

The "Metal poles" object has a **Racetrack Spacing Group** script component. This indicates that the content underneath will be spaced evenly along the track, and specifies the spacing.

The "Index" property is important to get correct spacing. Objects spaced at *different* intervals should be assigned to spacing groups with *different* indices, in order to get correct results. You should organise your spaced objects into distinct groups, each with a unique index between 0 and 15. The supplied mesh templates use index 0 for road support poles and index 1 for poles that hold up the side barriers.

The other properties are:
* Spacing Before - Amount of space to add before an object
* Spacing After - Amount of space to add after an object

In this case we have 10 units before and after each, meaning the poles will be spaced 20 units apart.

Underneath the "Metal poles" object we have two child objects, "left" and "right". Each has a **Racetrack Spaced** script component, indicating it has content to be repeated, with properties describing how:
* Is Vertical - This forces the object to be aligned vertically in world space. If unticked the object will be aligned with surface of the curves.
* Max Z angle - Maximum bank angle (positive or negative). If the curve exceeds this angle, the spaced content will not be created.
* Max X angle - Maximum pitch angle (positive or negative). Same as above.

*Important: "Max Z angle" and "Max X angle"p[] only apply when "Is Vertical" is ticked.*

In this case "Is Vertical" is ticked, so that the poles remain vertical regardless of how the road surface pitches or banks. The max X and Z angles ensure poles are not created if the track is upside down. (See the loop in the example scene for an example of upside-down track).

Underneath the "left" and "right" objects is the content to be generated. In this case we have a standard Unity mesh to represent the pole visually, and a capsule collider.

To complete the mesh template, add the side barriers as follows:
1. Open the "Assets/Racetrack builder/Prefabs/Template parts/Sides" project folder.
2. Drag the "rail barrier" prefab and drop it on the "template base" object in the scene tree. **Do not** drop it on the "continuous" or "spaced" child objects.

The prefab should not be a child of the "spaced" or "continuous" objects, because it contains both spaced and continuous content. If you expand the "rail barrier" object, you'll see it has its own "continuous" and "spaced" sub objects with the appropriate components attached to each. The "continuous" child object contains two railing meshes, which will be warped along the curve path. The "spaced" child object contains a small pole configured to be spaced every 6 units along the track.

The barrier runs along one side of the mesh template. To create the barrier on the other side:
1. Right click the "rail barrier" object in the scene tree and select "Duplicate"
2. In the Inspector window, set the rotation Y component to 180.

![New mesh template with barriers](images/newtemplatebarriers.jpg)

The mesh template is ready to use. The last step is to convert it into a prefab, so that it can be reused easily:
1. Click on the "template base" object in the scene tree.
2. Key in a new name in the Inspector window. E.g. "woodplank barriers"
3. Navigate to the "Assets/Racetrack builder/Prefabs/Track Templates" project folder.
4. Drag the new object from the scene tree into the project window to create a new template.
5. When prompted click "Prefab Variant" (although it doesn't make much difference in this case).

Once the prefab has been created, you can delete the object from your scene.

The template is now complete. Drag it from the project folder onto the "Template" field of a racetrack curve and click "Rest of track" to apply it to your track.

![New mesh template in use](images/newtemplateinuse.jpg)

Creating meshes
===============================================================================

You can of course use your own meshes to create track templates and build your own custom tracks.

The standard Unity workflow applies. I.e. create them in a 3rd party modeller, import them, and use them to build your mesh template prefabs.

For continuous meshes, like the road surface, there are some guidelines to ensure they will warp around the racetrack curves correctly, and provide a smooth driving experience.

Split along the Z axis
----------------------

Meshes are warped to fit a curve by transforming their existing vertices. It does not split existing polygons or introduce new vertices, so you must provide sufficient vertices to make the result look smooth.

![Visual mesh](images/visualmesh.jpg)

Here the road surface mesh has been sliced into 8 pieces, at regular intervals along the Z axis. (In this case the Blender3D "Loop cut" tool was used. Other 3D modelers should have a similar function.)

This is the visual mesh used for the "woodplank" road surface.

Separate high poly mesh for collisions
--------------------------------------

The visual mesh *looks* decent when warped around the racetrack curves, but does not have adequate detail to be used as the collision model. It will feel juddery to drive over when the road pitches or banks.

To fix this, create a second mesh, identical to the first, but with more polygons and vertices, so that it can be warped into a smoother surface.

![Collision mesh](images/collisionmesh.jpg)

Assign this mesh explicitly to the "Mesh Collider" component of your mesh object or prefab.

*Note: For continuous meshes that the player will **not** be driving on, like barriers or walls, it is not necessary to have a separate high poly mesh*

Use a mesh collider
-------------------

For continuous meshes the collision mesh must be warped along the racetrack curves along with the visible mesh. Although other colliders, like the box collider, may fit neatly around the mesh template, they cannot be warped, which will result in an incorrect collision model on your racetrack.

Therefore always use a mesh collider with continuous meshes.

Align the top of drivable surfaces with the Y=0 plane
-----------------------------------------------------

This is more of a suggestion than a rule, however it makes life a lot easier.

It means a continuous surface when the track switches from one mesh template to another. The road surface will also remain at the same height if you scale the mesh template to a different size (unlike if you aligned the surface to Y=1 for example).

Spaced objects
--------------

Spaced objects are much simpler than continuous meshes. There is no mesh warping. They are simply instantiated (using standard Unity object instantiation) and positioned at regular intervals along the track.

This means:
* You do not need to add extra vertices.
* You can freely use box colliders, capsule colliders.
