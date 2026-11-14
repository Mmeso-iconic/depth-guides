using UnityEngine;
using System.Collections.Generic;

// Depth Guides: a mini 3D placement experiment.
// The user moves a blue cube to match the position of a red sphere in 3D.
// With guides ON, both objects show a vertical "pole" down to the floor and a
// "shadow" disc on the floor. These are extra depth cues. The script records
// placement error and time for each trial, with and without guides.
//
// Setup: attach this script to an empty GameObject in a new 3D (URP) scene.
// Everything else (floor, objects, guides, camera position) is created by code.

public class DepthTask : MonoBehaviour
{
    public float moveSpeed = 2f;   // how fast the cube moves (units per second)
    public bool guidesOn = false;  // are the depth guides visible?

    GameObject cursorObj, targetObj;            // blue cube (user) and red sphere (target)
    GameObject cursorShadow, targetShadow;      // floor discs
    GameObject cursorPole, targetPole;          // vertical lines to the floor

    float trialStart;                           // time when the current trial began
    string lastResult = "Place the blue cube inside the red sphere, then press Space.";

    // Results are stored separately for each condition.
    List<float> errorsOn = new List<float>(), errorsOff = new List<float>();
    List<float> timesOn = new List<float>(), timesOff = new List<float>();

    Color blue = new Color(0.3f, 0.8f, 1f);
    Color red = new Color(1f, 0.35f, 0.3f);

    void Start()
    {
        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(2f, 1f, 2f);
        SetColor(floor, new Color(0.18f, 0.18f, 0.22f));

        // Target (red sphere) and cursor (blue cube)
        targetObj = MakeShape(PrimitiveType.Sphere, Vector3.one * 0.4f, red);
        cursorObj = MakeShape(PrimitiveType.Cube, Vector3.one * 0.3f, blue);

        // Guides: a flat disc on the floor and a thin pole for each object
        targetShadow = MakeShape(PrimitiveType.Cylinder, new Vector3(0.4f, 0.005f, 0.4f), red);
        cursorShadow = MakeShape(PrimitiveType.Cylinder, new Vector3(0.4f, 0.005f, 0.4f), blue);
        targetPole = MakeShape(PrimitiveType.Cylinder, new Vector3(0.03f, 1f, 0.03f), red);
        cursorPole = MakeShape(PrimitiveType.Cylinder, new Vector3(0.03f, 1f, 0.03f), blue);

        // Camera looks at the scene from the front, slightly above
        Camera.main.transform.position = new Vector3(0f, 3f, -7f);
        Camera.main.transform.LookAt(new Vector3(0f, 1f, 1f));

        NewTrial();
    }

    void Update()
    {
        // Movement: W/S = forward/back (depth), A/D = left/right, E/Q = up/down
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) move.x -= 1;
        if (Input.GetKey(KeyCode.D)) move.x += 1;
        if (Input.GetKey(KeyCode.W)) move.z += 1;
        if (Input.GetKey(KeyCode.S)) move.z -= 1;
        if (Input.GetKey(KeyCode.E)) move.y += 1;
        if (Input.GetKey(KeyCode.Q)) move.y -= 1;

        Vector3 p = cursorObj.transform.position + move * moveSpeed * Time.deltaTime;
        p.y = Mathf.Max(p.y, 0.15f); // do not go below the floor
        cursorObj.transform.position = p;

        // G switches the guides on/off and starts a fresh trial
        if (Input.GetKeyDown(KeyCode.G))
        {
            guidesOn = !guidesOn;
            NewTrial();
        }

        // Space confirms the placement
        if (Input.GetKeyDown(KeyCode.Space)) Submit();

        UpdateGuide(cursorObj, cursorShadow, cursorPole);
        UpdateGuide(targetObj, targetShadow, targetPole);
    }

    void NewTrial()
    {
        // Put the target somewhere random, and reset the cube
        targetObj.transform.position = new Vector3(
            Random.Range(-2.5f, 2.5f),
            Random.Range(0.5f, 2.5f),
            Random.Range(-1f, 4f));
        cursorObj.transform.position = new Vector3(0f, 1f, -2f);
        trialStart = Time.time;
    }

    void Submit()
    {
        float error = Vector3.Distance(cursorObj.transform.position, targetObj.transform.position);
        float time = Time.time - trialStart;

        if (guidesOn) { errorsOn.Add(error); timesOn.Add(time); }
        else { errorsOff.Add(error); timesOff.Add(time); }

        lastResult = "Last trial  |  Error: " + error.ToString("F2") + " units  |  Time: " + time.ToString("F1") + " s";
        NewTrial();
    }

    void UpdateGuide(GameObject obj, GameObject shadow, GameObject pole)
    {
        shadow.SetActive(guidesOn);
        pole.SetActive(guidesOn);
        if (!guidesOn) return;

        Vector3 p = obj.transform.position;
        shadow.transform.position = new Vector3(p.x, 0.01f, p.z);
        // A Unity cylinder is 2 units tall, so scale.y = height / 2
        pole.transform.position = new Vector3(p.x, p.y / 2f, p.z);
        pole.transform.localScale = new Vector3(0.03f, p.y / 2f, 0.03f);
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 18;
        GUI.skin.box.fontSize = 18;
        GUI.Box(new Rect(10, 10, 620, 210), "");
        GUI.Label(new Rect(20, 15, 600, 30), "DEPTH GUIDES  |  Guides: " + (guidesOn ? "ON" : "OFF") + "  (press G)");
        GUI.Label(new Rect(20, 45, 600, 30), "Move: W/A/S/D   Up/Down: E/Q   Place: Space");
        GUI.Label(new Rect(20, 80, 600, 30), lastResult);
        GUI.Label(new Rect(20, 120, 600, 30), "Guides OFF: " + Summary(errorsOff, timesOff));
        GUI.Label(new Rect(20, 150, 600, 30), "Guides ON:  " + Summary(errorsOn, timesOn));
    }

    string Summary(List<float> errors, List<float> times)
    {
        if (errors.Count == 0) return "no trials yet";
        return errors.Count + " trials, avg error " + Average(errors).ToString("F2") +
               ", avg time " + Average(times).ToString("F1") + " s";
    }

    float Average(List<float> list)
    {
        float sum = 0f;
        foreach (float v in list) sum += v;
        return sum / list.Count;
    }

    GameObject MakeShape(PrimitiveType type, Vector3 scale, Color color)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.transform.localScale = scale;
        Destroy(g.GetComponent<Collider>()); // no physics needed
        SetColor(g, color);
        return g;
    }

    void SetColor(GameObject g, Color c)
    {
        g.GetComponent<Renderer>().material.color = c;
    }
}
