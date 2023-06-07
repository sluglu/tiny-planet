using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System;
using static Planet;


public class Selection : MonoBehaviour
{
    Camera cam;
    public LineRenderer LineRenderer;
    Color32 colorOcean = new Color32(  0,  80, 220,   0);
    Color32 colorGrass = new Color32(  0, 220,   0,   0);
    Color32 colorDirt  = new Color32(180, 140,  20,   0);
    public float mouseSpeed = 3f;
    public float zoomSpeed = 0.1f;
    public float zoom = 150f;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (gameObject.GetComponent<LineRenderer> () == null){
            LineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        else {
            LineRenderer = gameObject.GetComponent<LineRenderer>();
        }  
        LineRenderer.startColor = Color.red;
        LineRenderer.endColor = Color.red;
        LineRenderer.startWidth = 0.1f;
        LineRenderer.endWidth = 0.1f;
        LineRenderer.positionCount = 4;
    }

    void LateUpdate()
    {
        if (Input.GetMouseButton(2)){
            if(Input.GetAxis("Mouse X")>0){
                transform.Translate(Vector3.left * mouseSpeed * Time.deltaTime);
            }
            if(Input.GetAxis("Mouse X")<0){
                transform.Translate(Vector3.right * mouseSpeed * Time.deltaTime);
            }
            if(Input.GetAxis("Mouse Y")>0){
                transform.Translate(Vector3.down * mouseSpeed * Time.deltaTime);
            }
            if(Input.GetAxis("Mouse Y")<0){
                transform.Translate(Vector3.up * mouseSpeed * Time.deltaTime);
            }
        }
        transform.LookAt(gameObject.transform.parent.position);
        if(Input.mouseScrollDelta.y>0){
            zoom += zoomSpeed;
        }
        if(Input.mouseScrollDelta.y<0){
            zoom -= zoomSpeed;
        }
        if(Vector3.Distance(gameObject.transform.position, gameObject.transform.parent.transform.position)<110f){
            zoom = 110f;
        }
        gameObject.transform.position = (gameObject.transform.position - gameObject.transform.parent.transform.position).normalized * zoom;
        
    }

    void Update()
    {
        RaycastHit hit;
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit)){
            if (LineRenderer.enabled){
                LineRenderer.enabled = !LineRenderer.enabled;
            }
            return;
        }

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null){
            if (LineRenderer.enabled){
                LineRenderer.enabled = !LineRenderer.enabled;
            }
            return;
        }
        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        List<Vector3> verts = new List<Vector3>{};
        verts.Add(vertices[hit.triangleIndex * 3 + 0]);
        verts.Add(vertices[hit.triangleIndex * 3 + 1]);
        verts.Add(vertices[hit.triangleIndex * 3 + 2]);
        Planet target = gameObject.transform.parent.GetComponent<Planet>();
        int test = 0;
        int count = 0;
        foreach(Polygon p in target.m_Polygons)
        {
            foreach(int vert in p.m_Vertices){
                if(target.m_Vertices[vert] == verts[0] || target.m_Vertices[vert] == verts[1] || target.m_Vertices[vert] == verts[2]){
                    test++;
                }
            }
            if(test == 3){
                break;
            }
            test = 0;
            count++;
        }
        if(target.m_sPolygons.Contains(target.m_Polygons[count]) == false){
            if (!LineRenderer.enabled){
                LineRenderer.enabled = !LineRenderer.enabled;
            }
            Polygon poly = target.m_Polygons[count];
            Transform hitTransform = hit.collider.transform;
            Vector3 p0 = hitTransform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 0]]);
            Vector3 p1 = hitTransform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 1]]);
            Vector3 p2 = hitTransform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 2]]);
            LineRenderer.SetPosition(0, p0);
            LineRenderer.SetPosition(1, p1);
            LineRenderer.SetPosition(2, p2);
            LineRenderer.SetPosition(3, p0);

            if (Input.GetMouseButtonDown(0)){
                //Stopwatch st = new Stopwatch();
                //st.Start();

                target.Extrude(poly);

                //st.Stop();
                //UnityEngine.Debug.Log(string.Format("Extrude : " + st.ElapsedMilliseconds));
                //st = new Stopwatch();
                //st.Start();

                target.meshUpdate();

                //st.Stop();
                //UnityEngine.Debug.Log(string.Format("meshUpdate : " + st.ElapsedMilliseconds));

            }
            if (Input.GetMouseButtonDown(1)){
                if(target.isOcean(poly) == false){
                    target.deExtrude(poly); 
                    target.meshUpdate();
                }
            }
        }
        else{
            if (LineRenderer.enabled){
                LineRenderer.enabled = !LineRenderer.enabled;
            }
        }
    }
}