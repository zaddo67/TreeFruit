using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeManager : MonoBehaviour
{

    [Tooltip("The players transform")]
    [SerializeField] public GameObject m_player;

    [Tooltip("Render prefabs within radius of player")]
    [SerializeField] public float m_radius;

    [Tooltip("Distance player needs to move before refreshing")]
    [SerializeField] public float m_refreshDistance;

    [Tooltip("The terrain to manager")]
    [SerializeField] public Terrain m_terrain;

    [Tooltip("The fruit prefab")]
    [SerializeField] public Transform m_fruit;

    [HideInInspector]
    [Tooltip("The tree prototype index")]
    [SerializeField] public int m_treeIndex;

    [HideInInspector]
    [Tooltip("The index's of the tree instances")]
    [SerializeField] public int[] m_instances;

    //[HideInInspector]
    [Tooltip("The positions of the tree instances")]
    [SerializeField] public Vector3[] m_positions;

    //[HideInInspector]
    [Tooltip("The rotations of the tree instances")]
    [SerializeField] public Vector3[] m_rotations;

    //[HideInInspector]
    [Tooltip("The scales of the tree instances")]
    [SerializeField] public Vector3[] m_scales;

    private Vector3 m_lastPosition = Vector3.zero;
    private bool[] m_treeState;
    private Dictionary<int, TerrainTree> m_activeTrees = new Dictionary<int, TerrainTree>();
    private int[] m_fruitVertices = new int[0];
    private Vector3[] m_vertexPos= new Vector3[0];
    private bool m_ready = false;

    // Start is called before the first frame update
    void Start()
    {
        if (m_terrain == null) return;
        if (m_fruit== null) return;


        m_treeState = new bool[m_instances.Length];
        for (int i=0; i<m_instances.Length;i++) m_treeState[i] = false;
        GetVertices();
		m_ready = true;

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!m_ready) return;
        if (m_player == null) return;

        if ((m_player.transform.position - m_lastPosition).magnitude > m_refreshDistance)
        {
            RefreshTrees();
            m_lastPosition = m_player.transform.position;
        }

        UpdateTrees();
    }

    private void UpdateTrees()
    {
        foreach (KeyValuePair<int, TerrainTree> pair in m_activeTrees)
        {
            UpdateTree(pair.Value);
        }
    }

    private void UpdateTree(TerrainTree treeInstance)
    {
        Vector3 pos = treeInstance.Instance.position;
        float rot = treeInstance.Instance.rotation;

        Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_rotations[treeInstance.TreeIndex]), m_scales[treeInstance.TreeIndex]);

        for (int i=0; i < m_fruitVertices.Length; i++)
        {
            var vertexPos = m.MultiplyPoint3x4(m_vertexPos[i]);
            treeInstance.Fruit[i].position = m_terrain.transform.TransformPoint(m_positions[treeInstance.TreeIndex] + vertexPos);
        }
    }

    private void RefreshTrees()
    {
        for (int i = 0; i < m_instances.Length; i++)
        {
            bool inRange = (m_player.transform.position - m_terrain.transform.TransformPoint(m_positions[i])).magnitude < m_radius;

            if (!inRange && m_treeState[i])
                TurnOffTree(i);   
            
            if (inRange && !m_treeState[i])
                TurnOnTree(i);
        }
    }

    private void TurnOffTree(int index)
    {
        m_treeState[index] = false;
        while (m_activeTrees[index].Fruit.Count> 0)
        {
            var fruit = m_activeTrees[index].Fruit[0];
            m_activeTrees[index].Fruit.Remove(fruit);
            GameObject.Destroy(fruit);
        }
        m_activeTrees.Remove(index);
    }

    private void TurnOnTree(int index)
    {
        m_treeState[index] = true;
        var instance = new TerrainTree();
        instance.TreeIndex = index;
        instance.Instance = m_terrain.terrainData.GetTreeInstance(index);
        for (int i = 0; i < m_fruitVertices.Length; i++)
        {
            var newFruit = Instantiate(m_fruit);
            newFruit.parent = this.transform;
            instance.Fruit.Add(newFruit);
        }

        m_activeTrees.Add(index, instance);
        UpdateTree(instance);
    }

    private void GetVertices()
    {
        var prototype = m_terrain.terrainData.treePrototypes[m_treeIndex];
        var treeFruit = prototype.prefab.transform.GetComponent<TreeFruit>();
        if (treeFruit == null)
        {
            m_fruitVertices = new int[0];
        }
        else
        {
            m_fruitVertices = treeFruit.m_modelVertices;
            m_vertexPos = treeFruit.m_VertexPos;
        }
    }

}

public class TerrainTree
{
    public int TreeIndex { get; set; }
    public TreeInstance Instance { get; set; }
    private List<Transform> m_fruit = new List<Transform>();
    public List<Transform> Fruit => m_fruit;
}
