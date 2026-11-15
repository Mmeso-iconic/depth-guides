using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// DEPTH GUIDES v3: Can one simple cue help first-timers judge depth in 3D?
//
// The player moves an orange cube to the exact spot of a teal ball inside a room,
// then presses PLACE. In one round the "guides" are ON (a pole down to the floor
// and a floor shadow under each object); in the other round they are OFF.
// For every placement it records the error split into DEPTH (near/far), SIDE
// (left/right) and HEIGHT, plus time. Results appear on screen for a screenshot,
// and on a laptop they are also saved to a CSV spreadsheet file.
//
// Works with a keyboard (W/A/S/D, E/Q, Space) AND with on-screen touch
// buttons, so it can be played in a phone browser after a Web build.
//
// Setup: attach this script to ONE empty GameObject in a new Universal 3D scene.
// Everything else (room, objects, guides, buddy, camera, lighting) is created by code.

public class DepthTask : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Calls NamePrompt.jslib, which opens the phone's own text box (so the keyboard appears)
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern string AskName(string current);
#endif

    enum State { Intro, Trial, Break, Question, Results }
    State state = State.Intro;

    public float moveSpeed = 1.6f;

    // Drag any Material from the Assets folder in here. Using a real material asset makes
    // sure its shader is included in the web build (otherwise everything shows up pink).
    public Material baseMaterial;
    const int TrialsPerBlock = 5;

    // Two fixed sets of target positions, so every player gets the same targets.
    Vector3[][] targetSets = new Vector3[][] {
        new Vector3[] { new Vector3(-2f, 1.8f, 3f), new Vector3(1.5f, 0.6f, 0.5f), new Vector3(2.2f, 2.2f, 3.5f), new Vector3(-1.2f, 0.9f, 1.5f), new Vector3(0.5f, 1.4f, 4f) },
        new Vector3[] { new Vector3(1.8f, 1.6f, 2.5f), new Vector3(-2.2f, 0.7f, 0.8f), new Vector3(-0.8f, 2.3f, 3.8f), new Vector3(2f, 1f, 1.2f), new Vector3(-0.4f, 1.2f, 3.2f) }
    };
    Vector3 startPos = new Vector3(0f, 1.2f, -1.5f);

    GameObject cursorObj, targetObj, cursorShadow, targetShadow, cursorPole, targetPole;
    GameObject buddyRoot; Vector3 buddyBase; float cheerTime = -10f;

    bool guidesOn;
    bool guidesFirst;            // decided by a coin flip, so practice does not favour one round
    string experience = "";      // "yes" or "no"
    string easier = "";          // which round felt easier
    int playerId;
    string playerName = "";      // typed in, or taken from the link (...?name=Ada)
    int block = 0, trial = 0, score = 0;
    float trialStart;
    string feedback = ""; float feedbackTime = -10f;
    bool placeHeld = false;

    // One record per placement: this is the data you analyse
    class Record { public int n; public bool guides; public float targetDepth, depthErr, sideErr, heightErr, totalErr, time; }
    List<Record> records = new List<Record>();

    Color orange = new Color(1f, 0.45f, 0.1f);
    Color teal = new Color(0.1f, 0.75f, 0.8f);

    // On-screen touch buttons (in a virtual screen that is always 720 units tall)
    float scale = 1f;
    Rect bLeft, bRight, bAway, bNear, bUp, bDown, bPlace;

    // ------------------------------------------------------------------ SETUP
    void Start()
    {
        // Real shadows are a depth cue too, so turn them off for a fair comparison
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.shadows = LightShadows.None;

        BuildRoom();
        BuildBuddy();

        targetObj = Make(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.45f, teal, null);
        cursorObj = Make(PrimitiveType.Cube, startPos, Vector3.one * 0.32f, orange, null);
        targetShadow = Make(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.45f, 0.005f, 0.45f), teal, null);
        cursorShadow = Make(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.45f, 0.005f, 0.45f), orange, null);
        targetPole = Make(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.03f, 1f, 0.03f), teal, null);
        cursorPole = Make(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.03f, 1f, 0.03f), orange, null);

        Camera cam = Camera.main;
        cam.transform.position = new Vector3(0f, 2.6f, -6f);
        cam.transform.LookAt(new Vector3(0f, 1.1f, 2.5f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1f, 0.95f, 0.9f);

        ShowObjects(false);
        playerId = Random.Range(1000, 10000);
        playerName = NameFromLink();
    }

    // If the game is opened with a link like .../depth-guides/?name=Ada, use that name
    string NameFromLink()
    {
        string url = Application.absoluteURL;
        int i = url.IndexOf("name=");
        if (i < 0) return "";
        string n = url.Substring(i + 5);
        int amp = n.IndexOf('&'); if (amp >= 0) n = n.Substring(0, amp);
        return System.Uri.UnescapeDataString(n.Replace("+", " "));
    }

    string ShownName() { return playerName.Trim() == "" ? "Player " + playerId : playerName.Trim(); }

    void BuildRoom()
    {
        Make(PrimitiveType.Cube, new Vector3(0f, -0.05f, 1.5f), new Vector3(10f, 0.1f, 10f), new Color(0.55f, 0.38f, 0.26f), null);   // wooden floor
        Make(PrimitiveType.Cube, new Vector3(0f, 3f, 6.55f), new Vector3(10.2f, 6f, 0.1f), new Color(0.98f, 0.93f, 0.86f), null);    // back wall (cream)
        Make(PrimitiveType.Cube, new Vector3(-5.05f, 3f, 1.5f), new Vector3(0.1f, 6f, 10f), new Color(0.85f, 0.45f, 0.3f), null);    // left wall (terracotta)
        Make(PrimitiveType.Cube, new Vector3(5.05f, 3f, 1.5f), new Vector3(0.1f, 6f, 10f), new Color(1f, 0.62f, 0.35f), null);       // right wall (soft orange)
        Make(PrimitiveType.Cube, new Vector3(-2.5f, 3.2f, 6.48f), new Vector3(2.2f, 1.4f, 0.05f), new Color(0.6f, 0.85f, 1f), null); // window
        Make(PrimitiveType.Cube, new Vector3(-4.98f, 2.6f, 3f), new Vector3(0.05f, 1.2f, 1.6f), new Color(0.2f, 0.25f, 0.35f), null); // picture frame
        Make(PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 2f), new Vector3(4.5f, 0.01f, 4.5f), new Color(1f, 0.85f, 0.7f), null); // rug
        Make(PrimitiveType.Cylinder, new Vector3(4f, 0.35f, 5.5f), new Vector3(0.6f, 0.35f, 0.6f), new Color(0.75f, 0.35f, 0.2f), null); // plant pot
        Make(PrimitiveType.Sphere, new Vector3(4f, 1.1f, 5.5f), new Vector3(1f, 1.2f, 1f), new Color(0.3f, 0.6f, 0.35f), null);       // plant
    }

    void BuildBuddy()
    {
        // A small friendly character that bobs and jumps when you do well
        buddyBase = new Vector3(-3.6f, 0f, 4.5f);
        buddyRoot = new GameObject("Buddy");
        buddyRoot.transform.position = buddyBase;
        Transform r = buddyRoot.transform;
        Color purple = new Color(0.55f, 0.45f, 0.95f);
        Make(PrimitiveType.Capsule, buddyBase + new Vector3(0f, 0.8f, 0f), new Vector3(0.7f, 0.8f, 0.7f), purple, r);
        Make(PrimitiveType.Sphere, buddyBase + new Vector3(0f, 1.9f, 0f), Vector3.one * 0.7f, purple, r);
        Make(PrimitiveType.Sphere, buddyBase + new Vector3(-0.13f, 1.95f, -0.3f), Vector3.one * 0.12f, Color.black, r);
        Make(PrimitiveType.Sphere, buddyBase + new Vector3(0.13f, 1.95f, -0.3f), Vector3.one * 0.12f, Color.black, r);
    }

    // ------------------------------------------------------------------ GAME LOOP
    void Update()
    {
        scale = Screen.height / 720f;
        LayoutButtons();

        // Buddy idles, and jumps when cheering
        float jump = (Time.time - cheerTime < 0.6f) ? Mathf.Sin((Time.time - cheerTime) / 0.6f * Mathf.PI) * 0.6f : 0f;
        buddyRoot.transform.position = buddyBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.05f + jump);

        if (state != State.Trial) return;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) move.x -= 1;
        if (Input.GetKey(KeyCode.D)) move.x += 1;
        if (Input.GetKey(KeyCode.W)) move.z += 1;
        if (Input.GetKey(KeyCode.S)) move.z -= 1;
        if (Input.GetKey(KeyCode.E)) move.y += 1;
        if (Input.GetKey(KeyCode.Q)) move.y -= 1;

        bool placeNow = Input.GetKeyDown(KeyCode.Space);
        bool placeDown = false;
        foreach (Vector2 p in Pointers())
        {
            if (bLeft.Contains(p)) move.x -= 1;
            if (bRight.Contains(p)) move.x += 1;
            if (bAway.Contains(p)) move.z += 1;
            if (bNear.Contains(p)) move.z -= 1;
            if (bUp.Contains(p)) move.y += 1;
            if (bDown.Contains(p)) move.y -= 1;
            if (bPlace.Contains(p)) placeDown = true;
        }
        if (placeDown && !placeHeld) placeNow = true;
        placeHeld = placeDown;

        Vector3 pos = cursorObj.transform.position + move.normalized * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -4.6f, 4.6f);
        pos.y = Mathf.Clamp(pos.y, 0.15f, 4f);
        pos.z = Mathf.Clamp(pos.z, -2.5f, 6f);
        cursorObj.transform.position = pos;
        cursorObj.transform.Rotate(0f, 30f * Time.deltaTime, 0f); // gentle spin, does not change position

        if (placeNow) Place();

        UpdateGuide(cursorObj, cursorShadow, cursorPole);
        UpdateGuide(targetObj, targetShadow, targetPole);
    }

    void StartBlock(int b)
    {
        block = b;
        trial = 0;
        if (b == 0) guidesFirst = Random.value < 0.5f;   // coin flip
        guidesOn = (b == 0) ? guidesFirst : !guidesFirst;
        state = State.Trial;
        ShowObjects(true);
        NextTarget();
    }

    void NextTarget()
    {
        targetObj.transform.position = targetSets[block][trial];
        cursorObj.transform.position = startPos;
        trialStart = Time.time;
    }

    void Place()
    {
        Vector3 c = cursorObj.transform.position, tg = targetObj.transform.position;
        float err = Vector3.Distance(c, tg);
        float t = Time.time - trialStart;

        // Split the error: depth (towards/away from you), side (left/right), height (up/down)
        Record r = new Record();
        r.n = records.Count + 1; r.guides = guidesOn; r.targetDepth = Vector3.Distance(Camera.main.transform.position, tg);
        r.depthErr = Mathf.Abs(c.z - tg.z); r.sideErr = Mathf.Abs(c.x - tg.x); r.heightErr = Mathf.Abs(c.y - tg.y);
        r.totalErr = err; r.time = t;
        records.Add(r);
        Debug.Log(CsvLine(r));

        score += Mathf.Max(0, Mathf.RoundToInt(100f - err * 100f));
        feedback = err < 0.15f ? "PERFECT!" : err < 0.35f ? "GREAT!" : err < 0.7f ? "GOOD" : "KEEP TRYING";
        feedbackTime = Time.time;
        if (err < 0.35f) cheerTime = Time.time;

        trial++;
        if (trial < TrialsPerBlock) { NextTarget(); return; }

        ShowObjects(false);
        state = (block == 0) ? State.Break : State.Question;
    }

    void Finish()
    {
        state = State.Results;
        SaveCsv();
    }

    void ResetAll()
    {
        records.Clear();
        score = 0; experience = ""; easier = ""; feedback = ""; playerName = "";
        playerId = Random.Range(1000, 10000);
        state = State.Intro;
    }

    // ------------------------------------------------------------------ HELPERS
    void UpdateGuide(GameObject obj, GameObject shadow, GameObject pole)
    {
        shadow.SetActive(guidesOn && obj.activeSelf);
        pole.SetActive(guidesOn && obj.activeSelf);
        if (!guidesOn) return;
        Vector3 p = obj.transform.position;
        shadow.transform.position = new Vector3(p.x, 0.03f, p.z);
        pole.transform.position = new Vector3(p.x, p.y / 2f, p.z);           // a Unity cylinder is 2 units tall
        pole.transform.localScale = new Vector3(0.03f, p.y / 2f, 0.03f);
    }

    void ShowObjects(bool show)
    {
        cursorObj.SetActive(show); targetObj.SetActive(show);
        if (!show) { cursorShadow.SetActive(false); targetShadow.SetActive(false); cursorPole.SetActive(false); targetPole.SetActive(false); }
    }

    GameObject Make(PrimitiveType type, Vector3 pos, Vector3 scl, Color c, Transform parent)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.transform.position = pos;
        g.transform.localScale = scl;
        Destroy(g.GetComponent<Collider>());
        Renderer rend = g.GetComponent<Renderer>();
        if (baseMaterial != null) rend.material = new Material(baseMaterial);
        rend.material.color = c;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        if (parent != null) g.transform.SetParent(parent, true);
        return g;
    }

    List<Vector2> Pointers()
    {
        // Every finger on the screen (or the mouse), converted to button coordinates
        List<Vector2> list = new List<Vector2>();
        for (int i = 0; i < Input.touchCount; i++) list.Add(ToGui(Input.GetTouch(i).position));
        if (Input.touchCount == 0 && Input.GetMouseButton(0)) list.Add(ToGui(Input.mousePosition));
        return list;
    }

    Vector2 ToGui(Vector2 screen) { return new Vector2(screen.x / scale, (Screen.height - screen.y) / scale); }

    void LayoutButtons()
    {
        float W = Screen.width / scale;
        bAway  = new Rect(130, 450, 90, 80);
        bLeft  = new Rect(30, 540, 90, 80);
        bRight = new Rect(230, 540, 90, 80);
        bNear  = new Rect(130, 625, 90, 80);
        bUp    = new Rect(W - 290, 450, 100, 80);
        bDown  = new Rect(W - 290, 545, 100, 80);
        bPlace = new Rect(W - 170, 470, 150, 150);
    }

    // Average of one measure, for guides ON or OFF
    float Avg(bool guides, System.Func<Record, float> pick)
    {
        float sum = 0f; int n = 0;
        foreach (Record r in records) if (r.guides == guides) { sum += pick(r); n++; }
        return n == 0 ? 0f : sum / n;
    }

    string CsvLine(Record r)
    {
        return ShownName().Replace(",", " ") + "," + playerId + "," + experience + "," + (guidesFirst ? "guides-first" : "no-guides-first") + "," + r.n + "," + (r.guides ? "ON" : "OFF") + "," +
               r.targetDepth.ToString("F2") + "," + r.depthErr.ToString("F2") + "," + r.sideErr.ToString("F2") + "," +
               r.heightErr.ToString("F2") + "," + r.totalErr.ToString("F2") + "," + r.time.ToString("F1");
    }

    void SaveCsv()
    {
        // On a laptop, every player is added to one spreadsheet file (CSV)
#if !UNITY_WEBGL
        string path = System.IO.Path.Combine(Application.persistentDataPath, "depth_guides_results.csv");
        if (!System.IO.File.Exists(path))
            System.IO.File.AppendAllText(path, "name,player_id,3d_experience,order,trial,guides,target_depth,depth_error,side_error,height_error,total_error,time_s,easier_round\n");
        foreach (Record r in records) System.IO.File.AppendAllText(path, CsvLine(r) + "," + easier + "\n");
        Debug.Log("Saved results to " + path);
#endif
    }

    // ------------------------------------------------------------------ SCREENS
    void OnGUI()
    {
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        float W = Screen.width / scale;

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUIStyle text = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        GUIStyle btn = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };
        title.normal.textColor = text.normal.textColor = small.normal.textColor = Color.white;

        if (state == State.Intro)
        {
            Panel(W);
            GUI.Label(new Rect(W / 2 - 400, 75, 800, 60), "DEPTH GUIDES", title);
            GUI.Label(new Rect(W / 2 - 380, 140, 760, 110), "Move your ORANGE cube to the exact spot of the TEAL ball, then press PLACE. It is harder than it looks! You will play 2 rounds of 5. In one round you get extra guides.", text);
            GUI.Label(new Rect(W / 2 - 380, 250, 200, 44), "Your name:", text);
#if UNITY_WEBGL && !UNITY_EDITOR
            // In the browser (phone or laptop): tap the button, the device's own keyboard opens
            if (GUI.Button(new Rect(W / 2 - 170, 250, 420, 44), playerName.Trim() == "" ? "Tap to type your name" : playerName, btn))
                playerName = AskName(playerName);
#else
            // In the Unity editor: type straight into the box
            GUIStyle field = new GUIStyle(GUI.skin.textField) { fontSize = 26 };
            playerName = GUI.TextField(new Rect(W / 2 - 170, 250, 420, 44), playerName, 24, field);
#endif
            GUI.Label(new Rect(W / 2 - 380, 305, 760, 40), "Have you played 3D games or used 3D apps before?", text);
            if (GUI.Button(new Rect(W / 2 - 170, 350, 150, 56), experience == "yes" ? "[ Yes ]" : "Yes", btn)) experience = "yes";
            if (GUI.Button(new Rect(W / 2 + 20, 350, 150, 56), experience == "no" ? "[ No ]" : "No", btn)) experience = "no";
            GUI.enabled = experience != "";
            if (GUI.Button(new Rect(W / 2 - 150, 425, 300, 72), "START", btn)) StartBlock(0);
            GUI.enabled = true;
            GUI.Label(new Rect(W / 2 - 380, 510, 760, 40), "Phone: hold it sideways and use the buttons.  Keyboard: W/A/S/D, E/Q, Space.", text);
            return;
        }

        if (state == State.Break)
        {
            Panel(W);
            GUI.Label(new Rect(W / 2 - 400, 140, 800, 60), "ROUND 1 DONE!", title);
            GUI.Label(new Rect(W / 2 - 380, 230, 760, 80), "Next round: guides " + (!guidesOn ? "ON. Look for the pole and the shadow on the floor." : "OFF. You are on your own this time!"), text);
            if (GUI.Button(new Rect(W / 2 - 150, 360, 300, 80), "CONTINUE", btn)) StartBlock(1);
            return;
        }

        if (state == State.Question)
        {
            Panel(W);
            GUI.Label(new Rect(W / 2 - 400, 140, 800, 60), "ONE LAST QUESTION", title);
            GUI.Label(new Rect(W / 2 - 380, 220, 760, 50), "Which round felt easier?", text);
            if (GUI.Button(new Rect(W / 2 - 390, 300, 250, 80), "With guides", btn)) { easier = "with guides"; Finish(); }
            if (GUI.Button(new Rect(W / 2 - 125, 300, 250, 80), "Without guides", btn)) { easier = "without guides"; Finish(); }
            if (GUI.Button(new Rect(W / 2 + 140, 300, 250, 80), "About the same", btn)) { easier = "same"; Finish(); }
            return;
        }

        if (state == State.Results)
        {
            GUI.Box(new Rect(W / 2 - 470, 10, 940, 700), "");
            GUI.Box(new Rect(W / 2 - 470, 10, 940, 700), "");
            GUIStyle row = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            row.normal.textColor = Color.white;
            GUIStyle head = new GUIStyle(row) { fontStyle = FontStyle.Bold };
            head.normal.textColor = new Color(1f, 0.7f, 0.45f);

            GUI.Label(new Rect(W / 2 - 400, 15, 800, 50), "RESULTS  |  " + ShownName() + "  (#" + playerId + ")", title);
            GUI.Label(new Rect(W / 2 - 450, 70, 900, 30), "3D experience: " + experience + "   |   Played " + (guidesFirst ? "guides ON first" : "guides OFF first") + "   |   Easier: " + easier + "   |   Score: " + score, row);

            // Summary: where do people go wrong, and do guides help?
            GUI.Label(new Rect(W / 2 - 450, 110, 900, 30), "AVERAGE ERROR          Depth (near/far)   Side (left/right)   Height    Time", head);
            GUI.Label(new Rect(W / 2 - 450, 140, 900, 30), "Guides OFF                    " + Avg(false, r => r.depthErr).ToString("F2") + "                    " + Avg(false, r => r.sideErr).ToString("F2") + "                    " + Avg(false, r => r.heightErr).ToString("F2") + "       " + Avg(false, r => r.time).ToString("F1") + " s", row);
            GUI.Label(new Rect(W / 2 - 450, 170, 900, 30), "Guides ON                     " + Avg(true, r => r.depthErr).ToString("F2") + "                    " + Avg(true, r => r.sideErr).ToString("F2") + "                    " + Avg(true, r => r.heightErr).ToString("F2") + "       " + Avg(true, r => r.time).ToString("F1") + " s", row);

            // Every placement, one row each
            GUI.Label(new Rect(W / 2 - 450, 215, 900, 30), "#    Guides   Target depth   Depth err   Side err   Height err   Total   Time", head);
            for (int i = 0; i < records.Count; i++)
            {
                Record rec = records[i];
                GUI.Label(new Rect(W / 2 - 450, 245 + i * 30, 900, 30),
                    rec.n + "     " + (rec.guides ? "ON " : "OFF") + "         " + rec.targetDepth.ToString("F1") + "               " + rec.depthErr.ToString("F2") + "          " +
                    rec.sideErr.ToString("F2") + "          " + rec.heightErr.ToString("F2") + "           " + rec.totalErr.ToString("F2") + "     " + rec.time.ToString("F1") + " s", row);
            }
            GUI.Label(new Rect(W / 2 - 450, 555, 900, 30), "Thank you, " + ShownName() + "! Please screenshot this whole screen and send it to Mmesoma.", head);
            if (GUI.Button(new Rect(W / 2 - 150, 600, 300, 70), "NEXT PLAYER", btn)) ResetAll();
            return;
        }

        // In a trial: info box, feedback, buddy speech and touch buttons
        GUI.Box(new Rect(10, 10, 560, 120), "");
        GUI.Label(new Rect(25, 18, 540, 32), "Round " + (block + 1) + " of 2   |   Target " + (trial + 1) + " of " + TrialsPerBlock, small);
        GUI.Label(new Rect(25, 52, 540, 32), "Guides: " + (guidesOn ? "ON" : "OFF") + "   |   Score: " + score, small);
        GUI.Label(new Rect(25, 86, 540, 32), "Line up the orange cube with the teal ball", small);

        if (Time.time - feedbackTime < 1.2f)
            GUI.Label(new Rect(W / 2 - 300, 170, 600, 70), feedback, title);

        if (Time.time - cheerTime < 1.2f)
        {
            Vector3 sp = Camera.main.WorldToScreenPoint(buddyRoot.transform.position + Vector3.up * 2.6f);
            Vector2 g = ToGui(sp);
            GUI.Box(new Rect(g.x - 60, g.y - 40, 120, 40), "Nice one!", new GUIStyle(GUI.skin.box) { fontSize = 22 });
        }

        GUI.Box(bAway, "AWAY", btn); GUI.Box(bLeft, "LEFT", btn); GUI.Box(bRight, "RIGHT", btn); GUI.Box(bNear, "NEAR", btn);
        GUI.Box(bUp, "UP", btn); GUI.Box(bDown, "DOWN", btn); GUI.Box(bPlace, "PLACE", btn);
    }

    void Panel(float W)
    {
        GUI.Box(new Rect(W / 2 - 420, 50, 840, 560), "");
        GUI.Box(new Rect(W / 2 - 420, 50, 840, 560), "");  // drawn twice so the panel is darker
    }
}
