using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JornadaSceneBuilder
{
    const string Gen = "Assets/Generated";
    const string Art = "Assets/Kenney/Art";
    const int LayerGround = 6, LayerPlayer = 7, LayerEnemy = 8;
    const float InicioSalao = -4f, FimSalao = 46f, PortaX = 40f;

    static readonly string[] Cenas = { "MainMenu", "Junior", "Pleno", "Senior", "Credits" };

    static Font fontePixel, fonteCodigo;
    static Sprite branco;
    static Material matFlash, matSprite;

    struct Inimigo
    {
        public string sprite, nome;
        public float x;
        public int hp, dano;
        public bool boss;
        public Inimigo(string sprite, string nome, float x, bool boss = false, int dano = 1)
        {
            this.sprite = sprite; this.nome = nome; this.x = x; this.boss = boss; this.dano = dano; hp = boss ? 3 : 2;
        }
    }

    [MenuItem("Jornada/Gerar Cenas")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Gen);
        ConfigurarProjeto();
        CarregarRecursos();
        var animHeroi = CriarAnimatorHeroi();
        var animInimigo = CriarAnimatorInimigo();

        CriarMenu();
        CriarFase(Tier.Junior, "junior", animHeroi, animInimigo,
            new Inimigo("slime", "Syntax Error", 14f),
            new Inimigo("aranha", "BOSS: Bug do Ponto Flutuante", 31f, true));
        CriarFase(Tier.Pleno, "pleno", animHeroi, animInimigo,
            new Inimigo("morcego", "NullPointerException", 12f),
            new Inimigo("rato", "Off-by-one", 21f),
            new Inimigo("caranguejo", "BOSS: Loop Infinito", 31f, true));
        CriarFase(Tier.Senior, "senior", animHeroi, animInimigo,
            new Inimigo("fantasma", "Memory Leak", 12f),
            new Inimigo("criatura", "Race Condition", 21f),
            new Inimigo("mago", "BOSS: Recursão Sem Base", 31f, true, 2));
        CriarCreditos();

        var lista = new EditorBuildSettingsScene[Cenas.Length];
        for (int i = 0; i < Cenas.Length; i++)
            lista[i] = new EditorBuildSettingsScene($"Assets/Scenes/{Cenas[i]}.unity", true);
        EditorBuildSettings.scenes = lista;
        AssetDatabase.SaveAssets();
        Debug.Log("Jornada: cenas geradas.");
    }

    static void ConfigurarProjeto()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(LayerGround).stringValue = "Ground";
        layers.GetArrayElementAtIndex(LayerPlayer).stringValue = "Player";
        layers.GetArrayElementAtIndex(LayerEnemy).stringValue = "Enemy";
        tagManager.ApplyModifiedProperties();

        Physics2D.IgnoreLayerCollision(LayerPlayer, LayerEnemy, true);
        Physics2D.IgnoreLayerCollision(LayerEnemy, LayerEnemy, true);

        PlayerSettings.productName = "Jornada do Dev";
        PlayerSettings.companyName = "Grupo P2";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        QualitySettings.antiAliasing = 0;
    }

    static void CarregarRecursos()
    {
        fontePixel = AssetDatabase.LoadAssetAtPath<Font>("Assets/Kenney/Fonts/KenneyPixel.ttf");
        // a fonte pixel desenha "*" e ")" de forma ambígua; o código precisa ser legível
        fonteCodigo = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        string caminhoBranco = $"{Gen}/branco.png";
        if (!File.Exists(caminhoBranco))
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            File.WriteAllBytes(caminhoBranco, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(caminhoBranco);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminhoBranco);
            imp.textureType = TextureImporterType.Sprite;
            imp.filterMode = FilterMode.Point;
            imp.SaveAndReimport();
        }
        branco = AssetDatabase.LoadAssetAtPath<Sprite>(caminhoBranco);

        matFlash = SalvarMaterial("Flash", Shader.Find("GUI/Text Shader"));
        matSprite = SalvarMaterial("SpriteDefault", Shader.Find("Sprites/Default"));
    }

    static Material SalvarMaterial(string nome, Shader shader)
    {
        string caminho = $"{Gen}/{nome}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat != null) return mat;
        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, caminho);
        return mat;
    }

    static Sprite S(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{caminho}.png");
    static AudioClip A(string nome) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Kenney/Audio/{nome}.ogg");

    // ---------- Animações ----------

    static AnimationClip Clip(string nome, bool loop, params (string prop, float[] tempos, float[] valores)[] curvas)
    {
        var clip = new AnimationClip { name = nome };
        foreach (var (prop, tempos, valores) in curvas)
        {
            var curva = new AnimationCurve();
            for (int i = 0; i < tempos.Length; i++) curva.AddKey(tempos[i], valores[i]);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), prop), curva);
        }
        var cfg = AnimationUtility.GetAnimationClipSettings(clip);
        cfg.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, cfg);
        AssetDatabase.CreateAsset(clip, $"{Gen}/{nome}.anim");
        return clip;
    }

    static AnimatorController CriarAnimatorHeroi()
    {
        string caminho = $"{Gen}/Heroi.controller";
        AssetDatabase.DeleteAsset(caminho);
        foreach (var n in new[] { "HeroiIdle", "HeroiRun", "HeroiJump", "HeroiAttack", "HeroiHurt" })
            AssetDatabase.DeleteAsset($"{Gen}/{n}.anim");

        var idle = Clip("HeroiIdle", true, ("m_LocalPosition.y", new[] { 0f, 0.4f, 0.8f }, new[] { 0f, 0.06f, 0f }));
        var run = Clip("HeroiRun", true,
            ("localEulerAnglesRaw.z", new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f }, new[] { 0f, -8f, 0f, 8f, 0f }),
            ("m_LocalPosition.y", new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f }, new[] { 0f, 0.12f, 0f, 0.12f, 0f }));
        var jump = Clip("HeroiJump", false,
            ("m_LocalScale.x", new[] { 0f, 0.1f }, new[] { 1f, 0.85f }),
            ("m_LocalScale.y", new[] { 0f, 0.1f }, new[] { 1f, 1.15f }));
        var attack = Clip("HeroiAttack", false,
            ("localEulerAnglesRaw.z", new[] { 0f, 0.08f, 0.18f, 0.3f }, new[] { 0f, 25f, -35f, 0f }));
        var hurt = Clip("HeroiHurt", false,
            ("localEulerAnglesRaw.z", new[] { 0f, 0.1f, 0.3f }, new[] { 0f, 20f, 0f }),
            ("m_LocalScale.y", new[] { 0f, 0.1f, 0.3f }, new[] { 1f, 0.85f, 1f }));

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(caminho);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sRun = sm.AddState("Run"); sRun.motion = run;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        var sAttack = sm.AddState("Attack"); sAttack.motion = attack;
        var sHurt = sm.AddState("Hurt"); sHurt.motion = hurt;
        sm.defaultState = sIdle;

        Transicao(sIdle, sRun).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Transicao(sRun, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        Transicao(sIdle, sJump).AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
        Transicao(sRun, sJump).AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
        Transicao(sJump, sIdle).AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
        Qualquer(sm, sAttack, "Attack");
        Qualquer(sm, sHurt, "Hurt");
        PorTempo(sAttack, sIdle);
        PorTempo(sHurt, sIdle);
        return ctrl;
    }

    static AnimatorController CriarAnimatorInimigo()
    {
        string caminho = $"{Gen}/Inimigo.controller";
        AssetDatabase.DeleteAsset(caminho);
        foreach (var n in new[] { "InimigoIdle", "InimigoChase", "InimigoDeath" })
            AssetDatabase.DeleteAsset($"{Gen}/{n}.anim");

        var idle = Clip("InimigoIdle", true,
            ("m_LocalScale.y", new[] { 0f, 0.5f, 1f }, new[] { 1f, 0.92f, 1f }),
            ("m_LocalScale.x", new[] { 0f, 0.5f, 1f }, new[] { 1f, 1.06f, 1f }));
        var chase = Clip("InimigoChase", true,
            ("localEulerAnglesRaw.z", new[] { 0f, 0.09f, 0.18f, 0.27f, 0.36f }, new[] { 0f, -10f, 0f, 10f, 0f }),
            ("m_LocalPosition.y", new[] { 0f, 0.09f, 0.18f, 0.27f, 0.36f }, new[] { 0f, 0.1f, 0f, 0.1f, 0f }));
        var death = Clip("InimigoDeath", false,
            ("localEulerAnglesRaw.z", new[] { 0f, 0.6f }, new[] { 0f, 90f }),
            ("m_LocalScale.y", new[] { 0f, 0.15f, 0.6f }, new[] { 1f, 1.3f, 0.1f }),
            ("m_LocalScale.x", new[] { 0f, 0.15f, 0.6f }, new[] { 1f, 0.7f, 0.1f }));

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(caminho);
        ctrl.AddParameter("Chasing", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sChase = sm.AddState("Chase"); sChase.motion = chase;
        var sDeath = sm.AddState("Death"); sDeath.motion = death;
        sm.defaultState = sIdle;
        Transicao(sIdle, sChase).AddCondition(AnimatorConditionMode.If, 0, "Chasing");
        Transicao(sChase, sIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "Chasing");
        Qualquer(sm, sDeath, "Death");
        return ctrl;
    }

    static AnimatorStateTransition Transicao(AnimatorState de, AnimatorState para)
    {
        var t = de.AddTransition(para);
        t.hasExitTime = false;
        t.duration = 0.05f;
        return t;
    }

    static void Qualquer(AnimatorStateMachine sm, AnimatorState para, string gatilho)
    {
        var t = sm.AddAnyStateTransition(para);
        t.hasExitTime = false;
        t.duration = 0f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, gatilho);
    }

    static void PorTempo(AnimatorState de, AnimatorState para)
    {
        var t = de.AddTransition(para);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.05f;
    }

    // ---------- Cenas ----------

    static Scene NovaCena() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    static void Salvar(Scene cena, string nome)
    {
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, $"Assets/Scenes/{nome}.unity");
    }

    static Camera CriarCamera(Color fundo, Vector3 pos)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = pos;
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fundo;
        go.AddComponent<AudioListener>();
        return cam;
    }

    static Color CorDoCeu(string fundo)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes($"{Art}/Backgrounds/{fundo}.png"));
        return tex.GetPixel(tex.width / 2, tex.height - 1);
    }

    static GameObject CriarFundo(string fundo, Transform cam, float y)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Fundo";
        Object.DestroyImmediate(go.GetComponent<MeshCollider>());
        go.transform.position = new Vector3(cam.position.x, y, 5f);
        go.transform.localScale = new Vector3(30f, 5f, 1f);
        var mat = new Material(Shader.Find("Unlit/Transparent"))
        {
            mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Backgrounds/{fundo}.png"),
            mainTextureScale = new Vector2(5f, 1f)
        };
        string caminho = $"{Gen}/Fundo_{fundo}.mat";
        AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(mat, caminho);
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = -20;
        var bg = go.AddComponent<BackgroundController>();
        Set(bg, "cameraTransform", cam);
        return go;
    }

    static SpriteRenderer Bloco(string nome, Sprite sprite, Vector2 centro, Vector2 tamanho, int ordem, Color? cor = null, Transform pai = null)
    {
        var go = new GameObject(nome);
        if (pai) go.transform.SetParent(pai);
        go.transform.position = centro;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = tamanho;
        sr.sortingOrder = ordem;
        sr.color = cor ?? Color.white;
        return sr;
    }

    static void CriarFase(Tier tier, string fundo, AnimatorController animHeroi, AnimatorController animInimigo, params Inimigo[] inimigos)
    {
        var cena = NovaCena();
        float largura = FimSalao - InicioSalao, centroX = (FimSalao + InicioSalao) / 2f;

        var cam = CriarCamera(CorDoCeu(fundo), new Vector3(5f, 3f, -10f));
        CriarFundo(fundo, cam.transform, 8.5f);

        var cenario = new GameObject("Cenario").transform;
        var chao = Bloco("Chao", S("Tiles/chao"), new Vector2(centroX, -0.5f), new Vector2(largura, 1f), 0, null, cenario);
        chao.gameObject.layer = LayerGround;
        chao.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(largura, 1f);
        Bloco("Terra", S("Tiles/terra"), new Vector2(centroX, -3f), new Vector2(largura, 4f), 0, null, cenario);
        Bloco("Parede", S("Tiles/parede"), new Vector2(centroX, 3f), new Vector2(largura, 6f), -10, new Color(0.55f, 0.55f, 0.65f), cenario);
        for (float x = 4f; x < PortaX; x += 8f)
        {
            Bloco("Coluna", S("Tiles/coluna"), new Vector2(x, 2.5f), new Vector2(1f, 5f), -5, null, cenario);
            Bloco("ColunaTopo", S("Tiles/coluna_topo"), new Vector2(x, 5.5f), Vector2.one, -5, null, cenario);
            if (x + 4f < PortaX) Bloco("Estandarte", S("Tiles/estandarte"), new Vector2(x + 4f, 3.5f), Vector2.one, -5, null, cenario);
        }
        Limite("LimiteEsquerdo", InicioSalao + 0.5f, cenario);
        Limite("LimiteDireito", FimSalao - 0.5f, cenario);

        var player = CriarHeroi(animHeroi);
        var camCtrl = cam.gameObject.AddComponent<CameraController>();
        Set(camCtrl, "targetPosition", player.transform);
        Set(camCtrl, "minX", InicioSalao + 9f);
        Set(camCtrl, "maxX", FimSalao - 9f);
        Set(camCtrl, "fixedY", 3f);

        foreach (var i in inimigos) CriarInimigo(i, player.transform, animInimigo);

        var porta = CriarPorta();

        var audio = new GameObject("Audio").AddComponent<AudioSource>();
        audio.playOnAwake = false;

        var sistemas = new GameObject("Sistemas");
        var fx = sistemas.AddComponent<CombatFx>();
        Set(fx, "flashMaterial", matFlash);
        Set(fx, "spriteMaterial", matSprite);
        Set(fx, "fonteNumeros", fontePixel);
        Set(fx, "audioSource", audio);
        Set(fx, "sfxSwing", A("swing"));
        Set(fx, "sfxHit", A("hit"));
        Set(fx, "sfxFinisher", A("finisher"));
        Set(fx, "sfxHurt", A("hurt"));

        var canvas = CriarCanvas();
        var hud = CriarHud(canvas.transform);
        var painel = CriarTaskPanel(canvas.transform, audio);
        var (fim, gameOver, vitoria) = CriarTelaFinal(canvas.transform);

        var gm = sistemas.AddComponent<GameManager>();
        Set(gm, "tier", (int)tier);
        Set(gm, "porta", porta);
        Set(gm, "telaFinal", fim);
        Set(gm, "audioSource", audio);
        Set(gm, "jingleInicio", A("jingle_inicio"));
        Set(gm, "jingleBossDerrotado", A("jingle_boss"));
        Set(gm, "jingleGameOver", A("jingle_gameover"));
        Set(gm, "jingleVitoria", A("jingle_vitoria"));

        var cm = sistemas.AddComponent<CombatManager>();
        Set(cm, "player", player);
        Set(cm, "taskPanel", painel);
        Set(cm, "fx", fx);

        Ligar(gameOver, "Tentar de novo", gm.Retry);
        Ligar(gameOver, "Menu", gm.Menu);
        Ligar(vitoria, "Créditos", gm.Creditos);
        Ligar(vitoria, "Menu", gm.Menu);

        CriarEventSystem();
        Salvar(cena, PhaseInfo.SceneFor(tier));
    }

    static void Limite(string nome, float x, Transform pai)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai);
        go.transform.position = new Vector3(x, 4f, 0f);
        go.layer = LayerGround;
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);
    }

    static GameObject Visual(GameObject raiz, Sprite sprite, AnimatorController anim, int ordem)
    {
        var vis = new GameObject("Visual");
        vis.transform.SetParent(raiz.transform, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = ordem;
        vis.AddComponent<Animator>().runtimeAnimatorController = anim;
        return vis;
    }

    static PlayerController CriarHeroi(AnimatorController anim)
    {
        var go = new GameObject("Heroi") { layer = LayerPlayer, tag = "Player" };
        go.transform.position = Vector3.zero;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.9f);
        col.offset = new Vector2(0f, 0.45f);
        col.sharedMaterial = SemAtrito();
        Visual(go, S("Characters/heroi"), anim, 10);

        var check = new GameObject("GroundCheck").transform;
        check.SetParent(go.transform, false);

        var pc = go.AddComponent<PlayerController>();
        Set(pc, "groundCheck", check);
        Set(pc, "groundLayer", 1 << LayerGround);
        go.AddComponent<PlayerCombatAnimator>();
        return pc;
    }

    static PhysicsMaterial2D SemAtrito()
    {
        string caminho = $"{Gen}/SemAtrito.physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(caminho);
        if (m) return m;
        m = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(m, caminho);
        return m;
    }

    static void CriarInimigo(Inimigo dados, Transform player, AnimatorController anim)
    {
        var go = new GameObject(dados.nome) { layer = LayerEnemy };
        go.transform.position = new Vector3(dados.x, 0f, 0f);
        if (dados.boss) go.transform.localScale = new Vector3(2f, 2f, 1f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.9f);
        col.offset = new Vector2(0f, 0.45f);
        col.sharedMaterial = SemAtrito();
        Visual(go, S($"Characters/{dados.sprite}"), anim, 9);

        var a = new GameObject(dados.nome + " - A").transform;
        a.position = new Vector3(dados.x - 1.5f, 0f, 0f);
        var b = new GameObject(dados.nome + " - B").transform;
        b.position = new Vector3(dados.x + 1.5f, 0f, 0f);

        var e = go.AddComponent<EnemyController>();
        Set(e, "displayName", dados.nome);
        Set(e, "isBoss", dados.boss);
        Set(e, "maxHp", dados.hp);
        Set(e, "damageToPlayer", dados.dano);
        Set(e, "player", player);
        Set(e, "pointA", a);
        Set(e, "pointB", b);
        Set(e, "detectionRange", dados.boss ? 6f : 5f);
        Set(e, "combatRange", dados.boss ? 2.2f : 1.5f);
    }

    static PhaseExit CriarPorta()
    {
        var go = new GameObject("Porta");
        go.transform.position = new Vector3(PortaX, 1f, 0f);
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S("Tiles/porta_fechada");
        sr.sortingOrder = -4;
        var gatilho = go.AddComponent<BoxCollider2D>();
        gatilho.isTrigger = true;
        gatilho.size = new Vector2(0.6f, 1f);

        var bloqueio = new GameObject("Bloqueio");
        bloqueio.transform.position = new Vector3(PortaX - 1.5f, 4f, 0f);
        bloqueio.layer = LayerGround;
        bloqueio.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 10f);

        var saida = go.AddComponent<PhaseExit>();
        Set(saida, "bloqueio", bloqueio);
        Set(saida, "porta", sr);
        Set(saida, "portaAberta", S("Tiles/porta_aberta"));
        return saida;
    }

    // ---------- UI ----------

    static void CriarEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static Canvas CriarCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform Rect(string nome, Transform pai, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = pivo;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        return rt;
    }

    static RectTransform Esticar(string nome, Transform pai)
    {
        var rt = Rect(nome, pai, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        return rt;
    }

    static Image Painel(RectTransform rt, Color cor)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = branco;
        img.color = cor;
        return img;
    }

    static Text Texto(RectTransform rt, string conteudo, int tamanho, Color cor, TextAnchor alinhamento = TextAnchor.MiddleCenter, Font fonte = null)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fonte ? fonte : fontePixel;
        t.text = conteudo;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = alinhamento;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Button Botao(string nome, Transform pai, Vector2 ancora, Vector2 pos, Vector2 tamanho, string rotulo, int fonte = 30, Font f = null)
    {
        var rt = Rect(nome, pai, ancora, new Vector2(0.5f, 0.5f), pos, tamanho);
        var img = Painel(rt, Color.white);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cores = btn.colors;
        cores.normalColor = new Color(0.22f, 0.24f, 0.36f);
        cores.highlightedColor = new Color(0.33f, 0.38f, 0.6f);
        cores.pressedColor = new Color(0.98f, 0.8f, 0.2f);
        cores.selectedColor = cores.normalColor;
        cores.disabledColor = new Color(0.16f, 0.16f, 0.2f);
        btn.colors = cores;
        var txt = Esticar("Texto", rt);
        txt.offsetMin = new Vector2(14, 0);
        txt.offsetMax = new Vector2(-14, 0);
        Texto(txt, rotulo, fonte, Color.white, TextAnchor.MiddleLeft, f);
        return btn;
    }

    static void Ligar(GameObject raiz, string rotulo, UnityAction acao)
    {
        foreach (var b in raiz.GetComponentsInChildren<Button>(true))
            if (b.GetComponentInChildren<Text>(true).text == rotulo)
                UnityEventTools.AddPersistentListener(b.onClick, acao);
    }

    static HudController CriarHud(Transform canvas)
    {
        var raiz = Esticar("HUD", canvas);
        var coracoes = Rect("Coracoes", raiz, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -16), new Vector2(400, 44));
        var grid = coracoes.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(40, 40);
        grid.spacing = new Vector2(6, 0);

        var titulo = Texto(Rect("Titulo", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(500, 50)),
            "", 40, new Color(1f, 0.85f, 0.3f));
        titulo.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

        var barra = Rect("BarraInimigo", raiz, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -14), new Vector2(380, 70));
        Texto(Rect("Nome", barra, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(380, 34)), "", 28, Color.white, TextAnchor.MiddleRight);
        var fundoBarra = Rect("Fundo", barra, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(380, 22));
        Painel(fundoBarra, new Color(0.1f, 0.1f, 0.12f));
        var preench = Esticar("Preenchimento", fundoBarra);
        preench.offsetMin = new Vector2(3, 3);
        preench.offsetMax = new Vector2(-3, -3);
        var img = Painel(preench, new Color(0.9f, 0.2f, 0.25f));
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;

        var hud = raiz.gameObject.AddComponent<HudController>();
        Set(hud, "painel", raiz);
        Set(hud, "coracoes", coracoes);
        Set(hud, "coracaoCheio", S("UI/coracao_cheio"));
        Set(hud, "coracaoVazio", S("UI/coracao_vazio"));
        Set(hud, "titulo", titulo);
        Set(hud, "barraInimigo", barra.gameObject);
        Set(hud, "nomeInimigo", barra.Find("Nome").GetComponent<Text>());
        Set(hud, "preenchimentoInimigo", img);
        barra.gameObject.SetActive(false);
        return hud;
    }

    static TaskPanel CriarTaskPanel(Transform canvas, AudioSource audio)
    {
        var host = Esticar("TaskPanel", canvas);
        var raiz = Rect("Janela", host, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(1200, 630));
        Painel(raiz, new Color(0.07f, 0.08f, 0.14f, 0.96f));
        raiz.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.85f, 0.3f);

        var top = new Vector2(0.5f, 1);
        var titulo = Texto(Rect("Titulo", raiz, top, top, new Vector2(0, -10), new Vector2(1140, 40)), "", 36, new Color(1f, 0.4f, 0.4f));
        var enunciado = Texto(Rect("Enunciado", raiz, top, top, new Vector2(0, -50), new Vector2(1140, 34)), "", 28, Color.white);

        var caixa = Rect("CaixaCodigo", raiz, top, top, new Vector2(0, -90), new Vector2(1140, 355));
        Painel(caixa, new Color(0.02f, 0.02f, 0.05f));
        var areaCodigo = Esticar("Codigo", caixa);
        areaCodigo.offsetMin = new Vector2(18, 10);
        areaCodigo.offsetMax = new Vector2(-18, -10);
        var codigo = Texto(areaCodigo, "", 24, new Color(0.95f, 0.95f, 0.95f), TextAnchor.UpperLeft, fonteCodigo);
        codigo.lineSpacing = 1.15f;

        var botoes = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -287f : 287f;
            float y = i < 2 ? -465f : -527f;
            botoes[i] = Botao($"Opcao{i + 1}", raiz, top, new Vector2(x, y), new Vector2(560, 54), $"{i + 1})", 24, fonteCodigo);
        }

        var feedback = Texto(Rect("Feedback", raiz, top, top, new Vector2(0, -590), new Vector2(1140, 56)), "", 24, Color.white);

        var painel = host.gameObject.AddComponent<TaskPanel>();
        Set(painel, "raiz", raiz.gameObject);
        Set(painel, "titulo", titulo);
        Set(painel, "enunciado", enunciado);
        Set(painel, "codigo", codigo);
        Set(painel, "botoes", botoes);
        Set(painel, "feedback", feedback);
        Set(painel, "audioSource", audio);
        Set(painel, "sfxClique", A("click"));
        Set(painel, "sfxCorreto", A("correto"));
        Set(painel, "sfxErrado", A("errado"));
        raiz.gameObject.SetActive(false);
        return painel;
    }

    static (EndScreen, GameObject, GameObject) CriarTelaFinal(Transform canvas)
    {
        var host = Esticar("TelaFinal", canvas);
        var gameOver = Sobreposicao(host, "GameOver", "GAME OVER", "O bug venceu desta vez. Revise o código e tente de novo!",
            new Color(1f, 0.3f, 0.3f), "Tentar de novo", "Menu");
        var vitoria = Sobreposicao(host, "Vitoria", "PROMOVIDO A DEV SENIOR!", "Você derrotou todos os bugs da jornada.",
            new Color(0.3f, 1f, 0.5f), "Créditos", "Menu");
        var fim = host.gameObject.AddComponent<EndScreen>();
        Set(fim, "gameOver", gameOver);
        Set(fim, "vitoria", vitoria);
        return (fim, gameOver, vitoria);
    }

    static GameObject Sobreposicao(Transform pai, string nome, string titulo, string sub, Color cor, string botao1, string botao2)
    {
        var rt = Esticar(nome, pai);
        Painel(rt, new Color(0f, 0f, 0f, 0.85f));
        var c = new Vector2(0.5f, 0.5f);
        var t = Texto(Rect("Titulo", rt, c, c, new Vector2(0, 120), new Vector2(1200, 100)), titulo, 80, cor);
        t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);
        Texto(Rect("Sub", rt, c, c, new Vector2(0, 40), new Vector2(1100, 50)), sub, 32, Color.white);
        Botao(botao1, rt, c, new Vector2(-170, -70), new Vector2(300, 64), botao1, 32);
        Botao(botao2, rt, c, new Vector2(170, -70), new Vector2(300, 64), botao2, 32);
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    static void CriarMenu()
    {
        var cena = NovaCena();
        var cam = CriarCamera(CorDoCeu("junior"), new Vector3(0f, 3f, -10f));
        CriarFundo("junior", cam.transform, 5f);
        Bloco("Chao", S("Tiles/chao"), new Vector2(0f, -0.5f), new Vector2(20f, 1f), 0);
        Bloco("Terra", S("Tiles/terra"), new Vector2(0f, -3f), new Vector2(20f, 4f), 0);
        Decoracao("Heroi", "Characters/heroi", -4f, 2.5f, false);
        Decoracao("Aranha", "Characters/aranha", 1.5f, 2f, true);
        Decoracao("Caranguejo", "Characters/caranguejo", 3.5f, 2f, true);
        Decoracao("Mago", "Characters/mago", 5.5f, 2f, true);

        var canvas = CriarCanvas();
        var raiz = Esticar("Menu", canvas.transform);
        var c = new Vector2(0.5f, 0.5f);
        var titulo = Texto(Rect("Titulo", raiz, c, c, new Vector2(0, 250), new Vector2(1200, 120)), "JORNADA DO DEV", 110, new Color(1f, 0.85f, 0.3f));
        titulo.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -5);
        Texto(Rect("Sub", raiz, c, c, new Vector2(0, 175), new Vector2(1100, 50)), "De Junior a Senior derrotando bugs com Python", 36, Color.white)
            .gameObject.AddComponent<Shadow>();

        var menu = raiz.gameObject.AddComponent<MenuController>();
        UnityEventTools.AddPersistentListener(Botao("Jogar", raiz, c, new Vector2(0, 80), new Vector2(320, 64), "Jogar", 34).onClick, menu.Play);
        UnityEventTools.AddPersistentListener(Botao("Creditos", raiz, c, new Vector2(0, 5), new Vector2(320, 64), "Créditos", 34).onClick, menu.Credits);
        UnityEventTools.AddPersistentListener(Botao("Sair", raiz, c, new Vector2(0, -70), new Vector2(320, 64), "Sair", 34).onClick, menu.Quit);

        Texto(Rect("Controles", raiz, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1200, 40)),
            "Mover: A/D ou setas   |   Pular: Espaço   |   Responder: clique ou teclas 1-4", 26, Color.white)
            .gameObject.AddComponent<Shadow>();

        CriarEventSystem();
        Salvar(cena, "MainMenu");
    }

    static void Decoracao(string nome, string sprite, float x, float escala, bool virar)
    {
        var go = new GameObject(nome);
        go.transform.position = new Vector3(x, 0f, 0f);
        go.transform.localScale = new Vector3(escala, escala, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S(sprite);
        sr.flipX = virar;
        sr.sortingOrder = 5;
    }

    static void CriarCreditos()
    {
        var cena = NovaCena();
        CriarCamera(new Color(0.07f, 0.08f, 0.14f), new Vector3(0f, 0f, -10f));
        var canvas = CriarCanvas();
        var raiz = Esticar("Creditos", canvas.transform);
        var c = new Vector2(0.5f, 0.5f);
        Texto(Rect("Titulo", raiz, c, c, new Vector2(0, 280), new Vector2(1200, 90)), "CRÉDITOS", 80, new Color(1f, 0.85f, 0.3f));
        Texto(Rect("Texto", raiz, c, c, new Vector2(0, 20), new Vector2(1100, 420)),
            "<b>Jornada do Dev</b>\nProjeto prático P2 - Desenvolvimento de Jogos Digitais - UNISAGRADO\n\n" +
            "<color=#ffd84d>Desenvolvimento</color>\n(integrantes do grupo)\n\n" +
            "<color=#ffd84d>Arte, fontes e áudio</color>\nKenney (www.kenney.nl) - licença Creative Commons CC0\n" +
            "Tiny Dungeon, Pixel Platformer, Impact Sounds, Interface Sounds, Music Jingles e Kenney Fonts\n\n" +
            "<color=#ffd84d>Feito com</color>\nUnity 6.3 LTS - tasks em Python 3.14",
            30, Color.white);
        var menu = raiz.gameObject.AddComponent<MenuController>();
        UnityEventTools.AddPersistentListener(Botao("Voltar", raiz, c, new Vector2(0, -290), new Vector2(320, 64), "Voltar ao menu", 32).onClick, menu.Menu);
        CriarEventSystem();
        Salvar(cena, "Credits");
    }

    // ---------- Serialização ----------

    static void Set(Object alvo, string campo, object valor)
    {
        var so = new SerializedObject(alvo);
        var p = so.FindProperty(campo);
        if (p == null)
        {
            Debug.LogError($"Campo '{campo}' não existe em {alvo.GetType().Name}");
            return;
        }
        switch (valor)
        {
            case Object[] objs:
                p.arraySize = objs.Length;
                for (int i = 0; i < objs.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
                break;
            case Object o: p.objectReferenceValue = o; break;
            case bool b: p.boolValue = b; break;
            case float f: p.floatValue = f; break;
            case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
            case int i when p.propertyType == SerializedPropertyType.LayerMask: p.intValue = i; break;
            case int i: p.intValue = i; break;
            case string s: p.stringValue = s; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
