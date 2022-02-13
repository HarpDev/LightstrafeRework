using System;
using System.Linq;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[InitializeOnLoad]
public class BlenderControls : MonoBehaviour
{
    private static Vector3 transformAxisLock;
    
    private static Vector3 movingStart;
    private static bool moving;
    private static Transform movingSelection;
    private static GameObject[] movingObjects;
    private static Vector2 movingAmount;

    [MenuItem("FzzyBlender/Move")]
    private static void Move()
    {
        if (Selection.activeTransform == null) return;
        movingSelection = Selection.activeTransform;
        movingObjects = Selection.gameObjects;

        var totalPosition = new Vector3();
        totalPosition = movingObjects.Aggregate(totalPosition, (current, obj) => current + obj.transform.position);
        totalPosition /= movingObjects.Length;
        
        movingStart = totalPosition;
        moving = true;
        movingAmount = Vector2.zero;
        transformAxisLock = Vector3.zero;
        Undo.SetCurrentGroupName("Object Move");
    }

    private static Quaternion rotatingStart;
    private static bool rotating;
    private static float rotateAmount;
    
    [MenuItem("FzzyBlender/Rotate")]
    private static void Rotate()
    {
        if (Selection.activeTransform == null) return;
        movingSelection = Selection.activeTransform;
        rotatingStart = movingSelection.rotation;
        rotateAmount = 0;
        rotating = true;
    }

    [MenuItem("FzzyBlender/FrameSelected")]
    private static void FrameSelected()
    {
        FrameSelected(Vector3.zero);
    }

    private static void FrameSelected(Vector3 zeroDirection, bool instant = false)
    {
        if (Selection.activeTransform == null) return;
        var objects = Selection.gameObjects;
        var bounds = new Bounds();
        var first = true;
        foreach (var obj in objects)
        {
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            else
            {
                var hitbox = obj.GetComponent<Collider>();
                if (hitbox == null)
                {
                    hitbox = obj.GetComponentInChildren<Collider>();
                }

                if (hitbox != null)
                {
                    if (first)
                    {
                        bounds = hitbox.bounds;
                        first = false;
                    }
                    else bounds.Encapsulate(hitbox.bounds);
                }
                else
                {
                    if (first)
                    {
                        first = false;
                        bounds.center = obj.transform.position;
                        bounds.size = Vector3.one;
                    }
                }
            }
        }

        if (zeroDirection.magnitude > 0)
        {
            zeroDirection = zeroDirection.normalized;
            var size = bounds.size;
            if (Mathf.Abs(zeroDirection.x) > 0.5f)
            {
                bounds.center += new Vector3(size.x / 2f * -Mathf.Sign(zeroDirection.x), 0, 0);
                size.x = 0;
            }
            else if (Mathf.Abs(zeroDirection.y) > 0.5f)
            {
                bounds.center += new Vector3(0, size.y / 2f * Mathf.Sign(zeroDirection.y), 0);
                size.y = 0;
            }
            else
            {
                bounds.center += new Vector3(0, 0, size.z / 2f * -Mathf.Sign(zeroDirection.z));
                size.z = 0;
            }

            bounds.size = size;
        }

        bounds.size *= 1.5f;

        SceneView.lastActiveSceneView.Frame(bounds, instant);
    }

    private static Vector3 lastViewPortRotation;

    private static Vector2 orthographicSelectorStart;
    private static bool waitingOnOrthographicSelector;

    private static int resistOrthoBreak;

    private static bool rotatingCamera;
    private static bool panningCamera;

    private static Vector2 prevMousePosition;

    private static bool shiftIsDown;
    private static bool ctrlIsDown;

    static BlenderControls()
    {
        SceneView.beforeSceneGui += view =>
        {
            var e = Event.current;
            if (e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.BackQuote && !waitingOnOrthographicSelector)
                {
                    waitingOnOrthographicSelector = true;
                    orthographicSelectorStart = Event.current.mousePosition;
                    e.Use();
                }

                if (e.keyCode == KeyCode.LeftShift)
                {
                    shiftIsDown = true;
                    e.Use();
                }

                if (e.keyCode == KeyCode.LeftControl)
                {
                    ctrlIsDown = true;
                    e.Use();
                }

                if (e.keyCode == KeyCode.X)
                {
                    if (moving || rotating) transformAxisLock = movingSelection.right;
                }
                if (e.keyCode == KeyCode.Y)
                {
                    if (moving || rotating) transformAxisLock = movingSelection.up;
                }
                if (e.keyCode == KeyCode.Z)
                {
                    if (moving || rotating) transformAxisLock = movingSelection.forward;
                }
            }
            if (e != null && e.type == EventType.KeyUp)
            {
                if (e.keyCode == KeyCode.LeftShift)
                {
                    shiftIsDown = false;
                    e.Use();
                }

                if (e.keyCode == KeyCode.LeftControl)
                {
                    ctrlIsDown = false;
                    e.Use();
                }
            }

            if (e != null && e.type == EventType.ScrollWheel)
            {
                if (e.delta.y > 0)
                {
                    var size = SceneView.lastActiveSceneView.size;
                    var reduction = size / 20;
                    SceneView.lastActiveSceneView.size += e.delta.y * reduction;
                }
                else
                {
                    var size = SceneView.lastActiveSceneView.size;
                    var reduction = size / 20;
                    SceneView.lastActiveSceneView.size += e.delta.y * reduction;
                }

                e.Use();
            }
            
            Tools.current = Tool.None;

            if (e != null && e.type == EventType.KeyUp)
            {
                if (e.keyCode == KeyCode.BackQuote)
                {
                    var selection = Event.current.mousePosition - orthographicSelectorStart;
                    selection.y *= -1;
                    var rotation = SceneView.lastActiveSceneView.rotation;
                    if (selection.magnitude > 20)
                    {
                        if (Vector2.Angle(selection, Vector2.up) < 45)
                        {
                            rotation.eulerAngles = new Vector3(90, rotation.eulerAngles.y, 0);
                        }
                        else if (Vector2.Angle(selection, Vector2.down) < 45)
                        {
                            rotation.eulerAngles = new Vector3(-90, 0, 0);
                        }
                        else if (Vector2.Angle(selection, Vector2.right) < 45)
                        {
                            rotation.eulerAngles = new Vector3(
                                Mathf.RoundToInt(rotation.eulerAngles.x / 90) * 90,
                                Mathf.RoundToInt(rotation.eulerAngles.y / 90) * 90,
                                Mathf.RoundToInt(rotation.eulerAngles.z / 90) * 90
                            );
                            rotation.eulerAngles += new Vector3(0, -90, 0);
                        }
                        else
                        {
                            rotation.eulerAngles = new Vector3(
                                Mathf.RoundToInt(rotation.eulerAngles.x / 90) * 90,
                                Mathf.RoundToInt(rotation.eulerAngles.y / 90) * 90,
                                Mathf.RoundToInt(rotation.eulerAngles.z / 90) * 90
                            );
                            rotation.eulerAngles += new Vector3(0, 90, 0);
                        }
                    }
                    else
                    {
                        rotation.eulerAngles = new Vector3(
                            Mathf.RoundToInt(rotation.eulerAngles.x / 90) * 90,
                            Mathf.RoundToInt(rotation.eulerAngles.y / 90) * 90,
                            Mathf.RoundToInt(rotation.eulerAngles.z / 90) * 90
                        );
                    }

                    SceneView.lastActiveSceneView.rotation = rotation;
                    SceneView.lastActiveSceneView.orthographic = true;
                    resistOrthoBreak = Environment.TickCount;
                    waitingOnOrthographicSelector = false;
                    //FrameSelected(-SceneView.lastActiveSceneView.rotation.eulerAngles);
                    var elevation = Mathf.Deg2Rad * rotation.eulerAngles.x;
                    var heading = Mathf.Deg2Rad * rotation.eulerAngles.y;
                    FrameSelected(new Vector3(Mathf.Cos(elevation) * Mathf.Sin(heading), Mathf.Sin(elevation),
                        Mathf.Cos(elevation) * Mathf.Cos(heading)), true);
                    e.Use();
                }
            }

            if (e != null && e.type == EventType.MouseUp)
            {
                if (e.button == 2)
                {
                    rotatingCamera = false;
                    panningCamera = false;
                    e.Use();
                }
            }

            if (e != null && e.type == EventType.MouseDown)
            {
                if (e.button == 2)
                {
                    if (shiftIsDown)
                    {
                        panningCamera = true;
                    }
                    else
                    {
                        rotatingCamera = true;
                    }
                    e.Use();
                }

                if (e.button == 0)
                {
                    if (moving)
                    {
                        moving = false;
                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        e.Use();
                    }

                    if (rotating)
                    {
                        rotating = false;
                        e.Use();
                    }
                }

                if (e.button == 1)
                {
                    if (moving)
                    {
                        moving = false;
                        movingSelection.position = movingStart;
                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        e.Use();
                    }

                    if (rotating)
                    {
                        rotating = false;
                        movingSelection.rotation = rotatingStart;
                        e.Use();
                    }
                }
            }

            if (rotatingCamera)
            {
                var mouseMovement = Event.current.mousePosition - prevMousePosition;
                var sensitivity = 0.4f;
                var rotation = SceneView.lastActiveSceneView.rotation;
                var euler = rotation.eulerAngles;
                euler.y += mouseMovement.x * sensitivity;
                if (euler.x > 180)
                {
                    if (euler.x + mouseMovement.y * sensitivity < 270)
                    {
                        euler.x = 270;
                    }
                    else
                    {
                        euler.x += mouseMovement.y * sensitivity;
                    }
                }
                else
                {
                    if (euler.x + mouseMovement.y * sensitivity > 90)
                    {
                        euler.x = 90;
                    }
                    else
                    {
                        euler.x += mouseMovement.y * sensitivity;
                    }
                }

                rotation.eulerAngles = euler;
                SceneView.lastActiveSceneView.rotation = rotation;
            } else if (panningCamera)
            {
                var mouseMovement = Event.current.mousePosition - prevMousePosition;
                var sensitivity = 0.4f;

                var screen = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(SceneView.lastActiveSceneView.pivot);

                screen.x -= mouseMovement.x;
                screen.y += mouseMovement.y;

                SceneView.lastActiveSceneView.pivot = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(screen);
            }

            if (rotating)
            {
                var selectedPosition = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(movingSelection.position);
                var selectedVec2 = new Vector2(selectedPosition.x, selectedPosition.y);
                var mousePos = Event.current.mousePosition;

                var fromSelect = selectedVec2 - mousePos;
                var prevFromSelect = selectedVec2 - prevMousePosition;
                
                var c = Mathf.Deg2Rad;
                var rotAmount = Mathf.Atan2(fromSelect.y * c, fromSelect.x * c) - Mathf.Atan2(prevFromSelect.y * c, prevFromSelect.x * c);

                rotateAmount += rotAmount * Mathf.Rad2Deg;

                movingSelection.rotation = rotatingStart;

                var snapping = ctrlIsDown ? rotateAmount - (rotateAmount % 15) : rotateAmount;
                movingSelection.Rotate(SceneView.lastActiveSceneView.camera.transform.forward, -snapping, Space.World);
            }
            
            if (moving)
            {
                var screen = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(movingStart);
                var mouseMovement = Event.current.mousePosition - prevMousePosition;

                movingAmount.x += mouseMovement.x;
                movingAmount.y -= mouseMovement.y;

                var targetScreen = new Vector3(screen.x + movingAmount.x, screen.y + movingAmount.y, screen.z);
                var movement = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(targetScreen) - movingStart;
                
                movingSelection.position = movingStart;

                if (transformAxisLock.sqrMagnitude > 0.1f)
                {
                    var amt = Vector3.Dot(transformAxisLock.normalized, movement);
                    movingSelection.position += transformAxisLock.normalized * amt;
                }
                else
                {
                    movingSelection.position += movement;
                }
                if (ctrlIsDown)
                {
                    var pos = movingSelection.position;
                    pos.x = Mathf.RoundToInt(pos.x);
                    pos.y = Mathf.RoundToInt(pos.y);
                    pos.z = Mathf.RoundToInt(pos.z);
                    movingSelection.position = pos;
                }
            }

            if (SceneView.lastActiveSceneView.camera.transform.rotation.eulerAngles != lastViewPortRotation
                && Environment.TickCount - resistOrthoBreak > 100)
            {
                SceneView.lastActiveSceneView.orthographic = false;
            }

            lastViewPortRotation = SceneView.lastActiveSceneView.camera.transform.rotation.eulerAngles;
            prevMousePosition = Event.current.mousePosition;
        };
    }
}