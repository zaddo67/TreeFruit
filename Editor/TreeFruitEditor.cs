using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.IO;
using Forge.Networking.Unity.Messages.Interpreters;
using System.Linq;

[CustomEditor(typeof(TreeFruit), true)]

public class TreeFruitEditor : Editor
{
    private List<int> m_modelVertices;
    private List<Vector3> m_vertexPos;
    private List<Transform> m_targets;

    bool m_vertexEdit = false; // show point editing tools
    bool m_needRepaint = true; // Update scene
    Mesh m_treeMesh;
    GameObject m_parentTarget;
    bool m_hasTarget = false;
    bool m_gizmosOn = false;
    Color m_saveColor= Color.white;
    TreeFruit p;

    bool VertexEdit
    {
        get { return m_vertexEdit; }
        set
        {
            if (VertexEdit == value) return;
            if (value && !m_hasTarget)
            {
                CreateTargets(p.transform);
                if (p.m_modelVertices != null)
                {
                    m_modelVertices = p.m_modelVertices.ToList();
                    m_vertexPos = p.m_VertexPos.ToList();
                }
                else
                {
                    m_modelVertices = new List<int>();
                    m_vertexPos = new List<Vector3>();
                }
            }
            if (!value) ClearTargets();
            m_vertexEdit = value;
            Tools.hidden = value; // hide tools
            m_needRepaint = true;
            SceneView.RepaintAll();
        }
    }
    public override void OnInspectorGUI()
    {

        m_saveColor = EditorStyles.label.normal.textColor;

        if (m_vertexEdit && !m_gizmosOn) GizmosMessage();

        p = (TreeFruit)target;
        if (p == null) return;

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        base.DrawDefaultInspector();

        string verb = m_vertexEdit ? "Stop" : "Start";
        VertexEdit = GUILayout.Toggle(VertexEdit, $"{verb} Vertex Capture", "Button");

        if (GUILayout.Button(new GUIContent("Clear")))
        {
            ResetTargets();
        }

        if (GUILayout.Button(new GUIContent("Test")))
        {
            TestTargets();
        }

        if (GUI.changed)
        {
            p.m_modelVertices = m_modelVertices.ToArray();
            p.m_VertexPos = m_vertexPos.ToArray();
        }

        // If any control changed, then apply changes
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void ResetTargets()
    {
        m_modelVertices.Clear();
        m_vertexPos.Clear();
        GUI.changed = true;
    }

    private void GizmosMessage()
    {
        //Label Color
        EditorStyles.label.normal.textColor = Color.red;

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"OnSceneGUI not active. Check Gizmos are turned on", GUILayout.MaxWidth(800));
        EditorGUILayout.EndVertical();

        EditorStyles.label.normal.textColor = m_saveColor;
    }


    void OnSceneGUI()
    {
        m_gizmosOn = true;
        Event guiEvent = Event.current;
        if (m_vertexEdit)
        {

            if (guiEvent.type == EventType.Repaint)
            {
                Draw(); //Draw Scene ponts and line
            }
            else if (guiEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else
            {
                HandlesInput(guiEvent); //Input Mouse
                if (m_needRepaint)
                    HandleUtility.Repaint();
            }
        }
    }


    private void Draw()
    {
        List<Vector3> vertices = new List<Vector3>();

        if (m_modelVertices == null) return;
        if (m_treeMesh== null) return;

        m_treeMesh.GetVertices(vertices);

        for (int i = 0; i < m_modelVertices.Count; i++)
        {
            Vector3 nextPoint = vertices[m_modelVertices[i]];

            Handles.color = p.m_gizmoColor;
            Handles.DrawSolidDisc(m_targets[m_modelVertices[i]].position, Camera.current.transform.forward, p.m_gizmoSize);


        }
        m_needRepaint = false;
    }

    private void HandlesInput(Event guiEvent)
    {
        if (guiEvent.type == EventType.MouseDown && m_vertexEdit)
        {
            if (m_vertexEdit)
            {
                if (guiEvent.button == 0)
                {
                    AppPoint(guiEvent);
                }
            }
        }
    }


    private void AppPoint(Event guiEvent)
    {
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        RaycastHit hit;
        float dstToDraw = (0 - mouseRay.origin.y) / mouseRay.direction.y;
        Vector3 point = mouseRay.GetPoint(dstToDraw);
        if (Physics.Raycast(mouseRay, out hit, 1000))
        {
            if (hit.transform.parent.name == "TreeFruitTemp")
            {
                Debug.Log($"Hit {hit.transform.name}");
                int vertex = int.Parse(hit.transform.name.Split('_')[1]);
                m_modelVertices.Add(vertex);
                m_vertexPos.Add(hit.transform.localPosition);
                GUI.changed = true;
            }
        }

        m_needRepaint = true;
    }

    private void TestTargets()
    {
        for (int i = 0; m_parentTarget.transform.childCount > 0; i++)
        {
            var child = m_parentTarget.transform.GetChild(i);
            int vertex = int.Parse(child.name.Split('_')[1]);

            m_modelVertices.Add(vertex);
            m_vertexPos.Add(child.localPosition);
            GUI.changed = true;
        }
    }


    private void CreateTargets(Transform treeTransform)
    {
        if (m_parentTarget != null) 
        {
            Debug.LogError($"Parent is already set");
            return; 
        }

        MeshFilter viewedModelFilter = GetMeshFilter(treeTransform);
        if (viewedModelFilter == null)
        {
            Debug.LogError($"MeshFilter not found on {treeTransform.name}");
            return;
        }

        m_hasTarget= true;

        // This should never happen. But if a Tree Manager already exists, then remove it
        var old = GameObject.Find("TreeFruitTemp");
        if (old != null) { m_parentTarget = old; ClearTargets(); }

        m_targets = new List<Transform>();

        m_parentTarget = new GameObject();
        m_parentTarget.name = "TreeFruitTemp";
        m_parentTarget.transform.parent = treeTransform;
        m_parentTarget.transform.localPosition = Vector3.zero;
        m_parentTarget.transform.localRotation = Quaternion.identity;
        m_treeMesh = viewedModelFilter.sharedMesh;


        for (int v = 0; v < m_treeMesh.vertices.Length; v++)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.parent = m_parentTarget.transform;
            target.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            target.transform.localPosition = m_treeMesh.vertices[v];
            target.transform.localRotation = Quaternion.identity;
            target.name = $"target_{v}";
            m_targets.Add(target.transform);
        }

        Debug.Log($"Targets Created");
    }

    private MeshFilter GetMeshFilter(Transform t)
    {
        MeshFilter meshFilter = (MeshFilter)t.GetComponent("MeshFilter");
        if (meshFilter != null) return meshFilter;
        for (int i=0; i<t.childCount; i++)
        {
            meshFilter = GetMeshFilter(t.GetChild(i));
            if (meshFilter != null) return meshFilter;
        }
        return null;
    }

    private void ClearTargets()
    {
        if (p.m_StopClear) return;
        Debug.Log($"Clear Targets");
        if (m_parentTarget != null)
        {
            DestroyImmediate(m_parentTarget);
            m_hasTarget = false;
            m_parentTarget= null;
        }
    }

    protected virtual void OnDisable()
    {
        Tools.hidden = false;
    }

    private void OnDestroy()
    {
        ClearTargets();
    }

}
