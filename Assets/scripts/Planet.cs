using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using static Perlin;

[ExecuteInEditMode]
public class Planet : MonoBehaviour
{
  [Range(1, 6)]
  public int resolution = 6;
  public float hillSize = 0.02f;
  [Range(1, 21)]
  public const int maxHeight = 21;
  public float noisePeriod = 3f;
  [Range(0, 1.5f)]
  public float verticalOffset = 0.75f;
  [Range(0, maxHeight)]
  public int oceanFloor = 1;
  [Range(0, maxHeight)]
  public int sandFloor = 2;
  [Range(0, maxHeight)]
  public int stoneFloor = 5;
  [Range(0, maxHeight)]
  public int snowFloor = 8;
  public string seed = "";

  Color32 colorOcean = new Color32(  0,  80, 220, 255);
  Color32 colorGrass = new Color32(  0, 220,   0, 255);
  Color32 colorDirt  = new Color32(180, 140,  20, 255);
  Color32 colorSand  = new Color32(234, 208, 168, 255);
  Color32 colorStone = new Color32(145, 142, 133, 255);
  Color32 colorSnow  = new Color32(255, 255, 255, 255);

  public List<Polygon> m_Polygons;
  public List<Polygon> m_fPolygons;
  public List<Polygon> m_sPolygons;
  public List<Polygon> s_Polygons;
  [System.NonSerialized]
  public List<Vector3> m_Vertices;
  GameObject m_PlanetMesh;
  MeshRenderer surfaceRenderer;
  MeshFilter terrainFilter;
  Mesh terrainMesh;
  MeshCollider meshCollider;


  public class Polygon
  { 
    public List<Polygon> initPolys;
    public List<int>     m_Vertices;
    public List<int>     oldVertices;
    public List<Polygon> m_Neighbors;
    public List<Polygon> stichedPolys;
    public List<Polygon> oldNeighbor;
    public Color32       m_Color;
    public int height;
    public Polygon(int a, int b, int c)
    {
      m_Vertices = new List<int>() { a, b, c };
      m_Neighbors = new List<Polygon>();
      oldNeighbor = new List<Polygon>();
      stichedPolys = new List<Polygon>();
      oldVertices = new List<int>();
      m_Color = new Color32(255, 0, 255, 255);
      initPolys = new List<Polygon>();
      height = 0;
    }

    public bool IsNeighborOf(Polygon other_poly)
    {
      int shared_vertices = 0;
      foreach (int vertex in m_Vertices)
      {
        if (other_poly.m_Vertices.Contains(vertex))
          shared_vertices++;
      }
      return shared_vertices == 2;
    }

    public void ReplaceNeighbor(Polygon oldNeighbor, Polygon newNeighbor)
    {
      for(int i = 0; i < m_Neighbors.Count; i++)
      {
        if(oldNeighbor == m_Neighbors[i])
        {
          m_Neighbors[i] = newNeighbor;
          return;
        }
      }
    }

    public EdgeSet CreateEdgeSet()
    {
      EdgeSet edgeSet = new EdgeSet();
      foreach (Polygon neighbor in this.m_Neighbors)
      {
        Edge edge = new Edge(this, neighbor);
        edgeSet.Add(edge);
      }
      return edgeSet;
    }

  }
  public class Edge
  {
          public Polygon m_InnerPoly; 
          public Polygon m_OuterPoly;
          public List<int> m_OuterVerts;
          public List<int> m_InnerVerts;
          public Edge(Polygon inner_poly, Polygon outer_poly)
          {
            m_InnerPoly  = inner_poly;
            m_OuterPoly  = outer_poly;
            m_OuterVerts = new List<int>(2);
            m_InnerVerts = new List<int>(2);
            foreach (int vertex in inner_poly.m_Vertices)
            {
              if (outer_poly.m_Vertices.Contains(vertex))
                m_InnerVerts.Add(vertex);
            }
            if(m_InnerVerts[0] == inner_poly.m_Vertices[0] &&
               m_InnerVerts[1] == inner_poly.m_Vertices[2])
            {
              int temp = m_InnerVerts[0];
              m_InnerVerts[0] = m_InnerVerts[1];
              m_InnerVerts[1] = temp;
            }
            m_OuterVerts = new List<int>(m_InnerVerts);
          }
  }
  public class EdgeSet : HashSet<Edge>
  {
          public void Split(List<int> oldVertices, List<int> newVertices)
          {
            foreach(Edge edge in this)
            {
              for(int i = 0; i < 2; i++)
              {
                edge.m_InnerVerts[i] = newVertices[oldVertices.IndexOf(edge.m_OuterVerts[i])];
              }
            }
          }
          public List<int> GetUniqueVertices()
          {
            List<int> vertices = new List<int>();
            foreach (Edge edge in this)
            {
              foreach (int vert in edge.m_OuterVerts)
              {
                if (!vertices.Contains(vert))
                  vertices.Add(vert);
              }
            }
            return vertices;
          }
  }
  int hashSeed(string seed){
    MD5 md5Hasher = MD5.Create();
    var hashed = md5Hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
    var ivalue = BitConverter.ToInt16(hashed, 0);
    return ivalue;
  }
  public bool isOcean(Polygon poly){
    if(poly.height <= oceanFloor){
      return true;
    }
    else{
      return false;
    }
  }  
  public bool isSand(Polygon poly){
    if(poly.height <= sandFloor && poly.height > oceanFloor){
      foreach(Polygon p in poly.initPolys){
        if(isOcean(p)){
          return true;
        }
        foreach(Polygon po in p.initPolys){
          if(isOcean(po)){
            return true;
          }
          //foreach(Polygon pol in po.initPolys){
          //  if(isOcean(pol)){
          //    return true;
          //  }
          //}
        }
      }
    }
    return false;
  }
  public bool isStone(Polygon poly){
    if(poly.height >= stoneFloor && poly.height < snowFloor){
      return true;
    }
    else{
      return false;
    }
  }
  public bool isSnow(Polygon poly){
    if(poly.height >= snowFloor){
      return true;
    }
    else{
      return false;
    }
  }
  void InitAsIcosahedron(){
            m_Polygons = new List<Polygon>();
            m_Vertices = new List<Vector3>();
            m_sPolygons = new List<Polygon>();
            m_fPolygons = new List<Polygon>();
            
            float t = (1.0f + Mathf.Sqrt (5.0f)) / 2.0f;
            m_Vertices.Add (new Vector3 (-1, t, 0).normalized);
            m_Vertices.Add (new Vector3 (1, t, 0).normalized);
            m_Vertices.Add (new Vector3 (-1, -t, 0).normalized);
            m_Vertices.Add (new Vector3 (1, -t, 0).normalized);
            m_Vertices.Add (new Vector3 (0, -1, t).normalized);
            m_Vertices.Add (new Vector3 (0, 1, t).normalized);
            m_Vertices.Add (new Vector3 (0, -1, -t).normalized);
            m_Vertices.Add (new Vector3 (0, 1, -t).normalized);
            m_Vertices.Add (new Vector3 (t, 0, -1).normalized);
            m_Vertices.Add (new Vector3 (t, 0, 1).normalized);
            m_Vertices.Add (new Vector3 (-t, 0, -1).normalized);
            m_Vertices.Add (new Vector3 (-t, 0, 1).normalized);
            m_Polygons.Add (new Polygon(0, 11, 5));
            m_Polygons.Add (new Polygon(0, 5, 1));
            m_Polygons.Add (new Polygon(0, 1, 7));
            m_Polygons.Add (new Polygon(0, 7, 10));
            m_Polygons.Add (new Polygon(0, 10, 11));
            m_Polygons.Add (new Polygon(1, 5, 9));
            m_Polygons.Add (new Polygon(5, 11, 4));
            m_Polygons.Add (new Polygon(11, 10, 2));
            m_Polygons.Add (new Polygon(10, 7, 6));
            m_Polygons.Add (new Polygon(7, 1, 8));
            m_Polygons.Add (new Polygon(3, 9, 4));
            m_Polygons.Add (new Polygon(3, 4, 2));
            m_Polygons.Add (new Polygon(3, 2, 6));
            m_Polygons.Add (new Polygon(3, 6, 8));
            m_Polygons.Add (new Polygon(3, 8, 9));
            m_Polygons.Add (new Polygon(4, 9, 5));
            m_Polygons.Add (new Polygon(2, 4, 11));
            m_Polygons.Add (new Polygon(6, 2, 10));
            m_Polygons.Add (new Polygon(8, 6, 7));
            m_Polygons.Add (new Polygon(9, 8, 1));
            
  }
  void Subdivide(){
    var midPointCache = new Dictionary<int, int>(); 
    for (int i = 0; i < resolution; i++)
    {
        var newPolys = new List<Polygon> ();
        foreach (var poly in m_Polygons)
        {
            int a = poly.m_Vertices [0];
            int b = poly.m_Vertices [1];
            int c = poly.m_Vertices [2];
            int ab = GetMidPointIndex (midPointCache, a, b);
            int bc = GetMidPointIndex (midPointCache, b, c);
            int ca = GetMidPointIndex (midPointCache, c, a);
            newPolys.Add (new Polygon (a, ab, ca));
            newPolys.Add (new Polygon (b, bc, ab));
            newPolys.Add (new Polygon (c, ca, bc));
            newPolys.Add (new Polygon (ab, bc, ca));
        }
        m_Polygons = newPolys;
        
    }
    foreach(Polygon p in m_Polygons){
      m_fPolygons.Add(p);
    }
  }
  int GetMidPointIndex (Dictionary<int, int> cache, int indexA, int indexB){
            int smallerIndex = Mathf.Min (indexA, indexB);
            int greaterIndex = Mathf.Max (indexA, indexB);
            int key = (smallerIndex << 16) + greaterIndex;
            int ret;
            if (cache.TryGetValue (key, out ret))
                return ret;
            Vector3 p1 = m_Vertices [indexA];
            Vector3 p2 = m_Vertices [indexB];
            Vector3 middle = Vector3.Lerp (p1, p2, 0.5f).normalized;

            ret = m_Vertices.Count;
            m_Vertices.Add (middle);

            cache.Add (key, ret);
            return ret;
  }
  void getSurfaceSidePolys(){
    s_Polygons = new List<Polygon>();

    foreach(Polygon poly in m_fPolygons){
      s_Polygons.Add(poly);
      int hf = 0;
      foreach(Polygon npoly in poly.initPolys){
        int h = poly.height - npoly.height;
        if(h > hf){hf = h;}
      }
      //UnityEngine.Debug.Log(hf + " | " + poly.stichedPolys.Count);
      int c = poly.stichedPolys.Count - 1;
      for(int i = 0; i < hf*6; i++){
        s_Polygons.Add(poly.stichedPolys[c-i]);
      }
    }
  }
  void GenerateMesh(){
    getSurfaceSidePolys();
    //UnityEngine.Debug.Log(s_Polygons.Count + " | " + m_Polygons.Count);
    if (gameObject.GetComponent<MeshRenderer> () == null){
      surfaceRenderer = gameObject.AddComponent<MeshRenderer>();
    }
    terrainMesh = new Mesh();
    terrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    int vertexCount = s_Polygons.Count * 3;
    int[] indices = new int[vertexCount];
    Vector3[] vertices = new Vector3[vertexCount];
    Vector3[] normals  = new Vector3[vertexCount];
    for (int i = 0; i < s_Polygons.Count; i++)
    {
      var poly = s_Polygons[i];
      indices[i * 3 + 0] = i * 3 + 0;
      indices[i * 3 + 1] = i * 3 + 1;
      indices[i * 3 + 2] = i * 3 + 2;
      vertices[i * 3 + 0] = m_Vertices[poly.m_Vertices[0]];
      vertices[i * 3 + 1] = m_Vertices[poly.m_Vertices[1]];
      vertices[i * 3 + 2] = m_Vertices[poly.m_Vertices[2]];
      normals[i * 3 + 0] = m_Vertices[poly.m_Vertices[0]];
      normals[i * 3 + 1] = m_Vertices[poly.m_Vertices[1]];
      normals[i * 3 + 2] = m_Vertices[poly.m_Vertices[2]];
    }
    terrainMesh.vertices = vertices;
    terrainMesh.normals = normals;
    terrainMesh.SetTriangles(indices, 0);
    if (gameObject.GetComponent<MeshFilter> () == null){
      terrainFilter = gameObject.AddComponent<MeshFilter>();
    }
    else{
      terrainFilter = gameObject.GetComponent<MeshFilter>();
    }
    terrainFilter.sharedMesh = terrainMesh;
    terrainFilter.sharedMesh.RecalculateNormals();
    if (gameObject.GetComponent<MeshCollider> () == null){
      meshCollider =  gameObject.AddComponent<MeshCollider>();
    }
    else {
      meshCollider = gameObject.GetComponent<MeshCollider>();
    }
    meshCollider.sharedMesh = terrainMesh;
  }
  void CalculateNeighbors(){
    List<Polygon> cPoly = new List<Polygon>();
    foreach(Polygon p in m_fPolygons){
      cPoly.Add(p);
    }
    foreach (Polygon poly in m_fPolygons)
    {
      for(int i = 0; i < cPoly.Count; i++)
      { 
        if(poly.m_Neighbors.Count == 3){
          break;
        }
        if (poly == cPoly[i]){
            continue;
        }
        if (poly.IsNeighborOf(cPoly[i])){
            poly.m_Neighbors.Add(cPoly[i]);
            poly.initPolys.Add(cPoly[i]);
            cPoly[i].m_Neighbors.Add(poly);
            cPoly[i].initPolys.Add(poly);
        }
      }
      cPoly.Remove(poly);
    }
  }
  List<int> CloneVertices(List<int> old_verts){
          List<int> new_verts = new List<int>();
          foreach(int old_vert in old_verts) 
          {
              Vector3 cloned_vert = m_Vertices [old_vert];
              new_verts.Add(m_Vertices.Count);
              m_Vertices.Add(cloned_vert);
          }
          return new_verts;
  }
  void StitchPolys(Polygon poly){
    //UnityEngine.Debug.Log("yeah");
    //UnityEngine.Debug.Log(poly.m_Neighbors.Count);
    var edgeSet = poly.CreateEdgeSet();
    var originalVerts = edgeSet.GetUniqueVertices();
    foreach(int vert in originalVerts){
      poly.oldVertices.Add(vert);
    }

    var newVerts = CloneVertices(originalVerts);
    edgeSet.Split(originalVerts, newVerts);
    foreach (Edge edge in edgeSet)
    {
      var stitch_poly1 = new Polygon(edge.m_OuterVerts[0],
                                     edge.m_OuterVerts[1],
                                     edge.m_InnerVerts[0]);
      var stitch_poly2 = new Polygon(edge.m_OuterVerts[1],
                                     edge.m_InnerVerts[1],
                                     edge.m_InnerVerts[0]);
      edge.m_InnerPoly.ReplaceNeighbor(edge.m_OuterPoly,
                                       stitch_poly2);
      edge.m_OuterPoly.ReplaceNeighbor(edge.m_InnerPoly,
                                       stitch_poly1);
      m_Polygons.Add(stitch_poly1);
      m_Polygons.Add(stitch_poly2);
      m_sPolygons.Add(stitch_poly1);
      m_sPolygons.Add(stitch_poly2);
      poly.stichedPolys.Add(stitch_poly1);
      poly.stichedPolys.Add(stitch_poly2);
      poly.oldNeighbor.Add(edge.m_OuterPoly);
    }

    for (int i = 0; i < 3; i++)
    {
      int vert_id = poly.m_Vertices[i];
      if (!originalVerts.Contains(vert_id))
        continue;
      
      int vert_index = originalVerts.IndexOf(vert_id);
      poly.m_Vertices[i] = newVerts[vert_index];
    }
  }
  public void Extrude(Polygon poly){
    StitchPolys(poly);
    List<int> verts = poly.m_Vertices;
    foreach (int vert in verts)
    {
      Vector3 v = m_Vertices[vert];
      v = v.normalized * (v.magnitude + hillSize);
      m_Vertices[vert] = v;
    }
    poly.height+=1;
  }
  public void deExtrude(Polygon poly){
    var edgeSet = poly.CreateEdgeSet();
    int count = 0;
    foreach (Edge edge in edgeSet)
    {
      edge.m_InnerPoly.ReplaceNeighbor(edge.m_OuterPoly, poly.oldNeighbor[poly.oldNeighbor.Count - 3 + count]);
      poly.oldNeighbor.RemoveAt(poly.oldNeighbor.Count - 3 + count);
      count++;
    }
    for(int i = 0; i < 6 ; i++){
      m_Polygons.Remove(poly.stichedPolys[poly.stichedPolys.Count - 6 + i]);
      m_sPolygons.Remove(poly.stichedPolys[poly.stichedPolys.Count - 6 + i]);
    }
    List<Polygon> newStit = new List<Polygon>();
    foreach(Polygon p in poly.stichedPolys){
      if(!m_sPolygons.Contains(p)){
        for(int i = 0; i < poly.stichedPolys.IndexOf(p); i++){
          newStit.Add(poly.stichedPolys[i]);
        }
        poly.stichedPolys = newStit;
        break;
      }
    }

    for (int i = 0; i < 3; i++)
    {
      poly.m_Vertices[i] = poly.oldVertices[poly.oldVertices.Count - 3 + i];
      poly.oldVertices.RemoveAt(poly.oldVertices.Count - 3 + i);
    }
    poly.height-=1;
  }
  List<Polygon> GetPolysInSphere(Vector3 center, float radius, IEnumerable<Polygon> source){
    List<Polygon> newSet = new List<Polygon>();
    foreach(Polygon p in source)
    {
      foreach(int vertexIndex in p.m_Vertices)
      {
        float distanceToSphere = Vector3.Distance(center,
                                 m_Vertices[vertexIndex]);
        if (distanceToSphere <= radius)
        {
          if(!m_sPolygons.Contains(p)){
            newSet.Add(p);
            break;
          }
        }
      }
    }
    return newSet;
  }
  void applyColor(Polygon poly){
    if(isOcean(poly)){
      poly.m_Color = colorOcean;
      foreach(Polygon p in poly.stichedPolys){
        p.m_Color = colorOcean;
      }
    }
    else if(isSand(poly)){
      poly.m_Color = colorSand;
      foreach(Polygon p in poly.stichedPolys){
        p.m_Color = colorSand;
      }
    }
    else if(isStone(poly)){
      poly.m_Color = colorStone;
      foreach(Polygon p in poly.stichedPolys){
        p.m_Color = colorStone;
      }
    }
    else if(isSnow(poly)){
      poly.m_Color = colorSnow;
      foreach(Polygon p in poly.stichedPolys){
        p.m_Color = colorStone;
      }
      for(int i = 0; i < 6; i++){
        poly.stichedPolys[poly.stichedPolys.Count - 6 + i].m_Color = colorSnow;
      }
    }
    else{
      poly.m_Color = colorGrass;
      foreach(Polygon p in poly.stichedPolys){
        p.m_Color = colorDirt;
      }
    }
  }
  void UpdateNoise(int s){
    Vector3 offset = new Vector3(s,s,s);
    foreach(Polygon poly in m_fPolygons){
      Vector3 center = new Vector3();
      foreach(int vert in poly.m_Vertices){
        center += m_Vertices[vert];
      }
      center.x = (center.x + offset.x) * noisePeriod;
      center.y = (center.y + offset.y) * noisePeriod;
      center.z = (center.z + offset.z) * noisePeriod;
      float noise = Noise(center) * 2 + verticalOffset;
      float height = noise*maxHeight;
      for(int o = 1; o < height; o++){
        Extrude(poly);
      }
    }
  }
  public void Generate(){
    //Stopwatch st = new Stopwatch();
    //st.Start();
    

    InitAsIcosahedron();

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("InitAsIcosahedron : " + st.ElapsedMilliseconds));
    //st = new Stopwatch();
    //st.Start();

    Subdivide();

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("Subdivide : " + st.ElapsedMilliseconds));
    //st = new Stopwatch();
    //st.Start();

    CalculateNeighbors();

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("CalculateNeighbors : " + st.ElapsedMilliseconds));
    //st = new Stopwatch();
    //st.Start();

    foreach(Polygon poly in m_fPolygons){
      Extrude(poly);
    }

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("Extrude : " + st.ElapsedMilliseconds));
    //st = new Stopwatch();
    //st.Start();
    if(int.TryParse(seed, out int n)){
      UpdateNoise(n);
    }
    else{
      UpdateNoise(hashSeed(seed));
    }
    

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("UpdateNoise : " + st.ElapsedMilliseconds));
    //st = new Stopwatch();
    //st.Start();

    meshUpdate();

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("meshUpdate : " + st.ElapsedMilliseconds));

  }
  public void Randomize(){
    InitAsIcosahedron();
    Subdivide();
    CalculateNeighbors();
    foreach(Polygon poly in m_fPolygons){
      Extrude(poly);
    }
    var buffer = new byte[sizeof(Int16)];
    System.Random rnd = new System.Random();
    rnd.NextBytes(buffer);
    BitConverter.ToInt16(buffer, 0);
    int s = BitConverter.ToInt16(buffer, 0);
    UpdateNoise(s);
    seed = s.ToString();
    meshUpdate();
  }
  public void clear(){
    InitAsIcosahedron();
    Subdivide();
    CalculateNeighbors();
    foreach(Polygon poly in m_fPolygons){
      Extrude(poly);
    }
    meshUpdate();
  }
  public void UpdateColors(){
    foreach(Polygon p in m_fPolygons)
    {
      applyColor(p);
    }
    int vertexCount = s_Polygons.Count * 3;
    terrainFilter = gameObject.GetComponent<MeshFilter>();
    terrainMesh = terrainFilter.sharedMesh;
    Color32[] colors   = new Color32[vertexCount];
    for (int i = 0; i < s_Polygons.Count; i++)
    {
      var poly = s_Polygons[i];
      colors[i * 3 + 0] = poly.m_Color;
      colors[i * 3 + 1] = poly.m_Color;
      colors[i * 3 + 2] = poly.m_Color;
    }
    terrainMesh.colors32 = colors;
  }
  public void meshUpdate(){
    //Stopwatch st = new Stopwatch();
    //st.Start();

    GenerateMesh();

    ///st.Stop();
    ///UnityEngine.Debug.Log(string.Format("GenerateMesh : " + st.ElapsedMilliseconds));
    ///st = new Stopwatch();
    ///st.Start();

    UpdateColors();

    //st.Stop();
    //UnityEngine.Debug.Log(string.Format("UpdateColors : " + st.ElapsedMilliseconds));
  }
  void Start(){
    Generate();
  }
}