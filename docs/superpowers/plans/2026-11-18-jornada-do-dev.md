# Jornada do Dev — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Jogo 2D 8-bit para PC (Unity) com 3 fases (Junior, Pleno, Senior). Em cada fase o jogador atravessa um salão com mobs e um boss. Cada combate abre uma task de Python: acertar faz o herói golpear, errar faz o herói tomar dano.

**Architecture:** Regras de jogo em classes C# puras (`Health`, `QuestionBank`, `CombatEncounter`), cobertas por testes EditMode. MonoBehaviours finos ligam essas regras à cena: player, inimigo com máquina de estados, combate, efeitos e HUD. As cenas são geradas por um script de Editor (`JornadaSceneBuilder`) para serem reproduzíveis em batchmode e versionáveis no git. As perguntas ficam num JSON e são validadas executando cada trecho no Python 3.14 real.

**Tech Stack:** Unity 6.3 LTS (6000.3.x), C#, Input System (wrapper `PlayerControls` gerado, como nas aulas), uGUI + TextMeshPro, Unity Test Framework (EditMode), Python 3.14 (só para validar o banco de perguntas), arte e sons CC0 da Kenney.

**Spec:** pedido do usuário nesta sessão, mais `Orientações - P2` (projeto prático, entrega e apresentação em 18/11/2026 e 25/11/2026). Resumo da spec no fim deste arquivo.

## Global Constraints

- Engine: Unity instalada no PC do usuário, linha 6000.3 (Hub já registra `6000.3.9f1`).
- Jogo 2D, PC (Windows), arte estilo 8-bit: sprites com `FilterMode.Point`, sem compressão, 16 PPU, câmera ortográfica.
- 3 fases, nesta ordem: `Junior`, `Pleno`, `Senior`. Cada uma tem 1 ou 2 mobs e 1 boss.
- Tasks em Python, versão mais recente instalada (`Python 3.14.7`). O jogador lê o código e responde em múltipla escolha (4 opções). O jogo não executa Python.
- Dificuldade progressiva: Junior começa em operações matemáticas, Pleno vai para strings, listas, condicionais e laços, Senior chega a funções.
- Acerto: herói golpeia (animação "performática"). Erro: herói toma golpe e perde vida. Vida 0: Game Over.
- Repositório git desde a v0: `https://github.com/IanAmaral12/jornada-do-dev.git` (privado, vazio, branch `main`).
- Comentários no código só quando necessários, em português BR.
- Assets de terceiros apenas CC0/livres, com fonte creditada numa tela de Créditos (exigência da P2).
- Entregáveis P2: build executável, projeto (GitHub), GDD simplificado, README com integrantes e instruções de execução.
- Usar tópicos das aulas (repos `LuisFilipeTinos/*`): Input System com `PlayerControls`, `Rigidbody2D` + ground check, Animator com parâmetros, câmera `SmoothDamp`, fundo com `mainTextureOffset`, máquina de estados de inimigo (`State`/`Enter`/`Execute`), `Death()` + `Destroy`, `SceneManager`, `GameManager` singleton com `Instancia`, `Time.timeScale`.

## Review Focus

1. **Banco de perguntas esgotado:** com 2 mobs + boss e erros repetidos, a fase pede mais perguntas do que existem no tier. O esperado é reembaralhar sem repetir a última pergunta, e nunca lançar exceção. Coberto em Task 3: `Next_WhenTierExhausted_ReshufflesWithoutImmediateRepeat`.
2. **Duplo clique ou tecla durante a animação:** responder de novo enquanto o golpe anima não pode aplicar dano duas vezes. Coberto em Task 3 (`Answer_WhenAlreadyResolved_IsIgnored`) e na trava de input da Task 7.
3. **`Time.timeScale` preso em 0:** hit-stop ou Game Over seguidos de troca de cena não podem congelar a próxima cena. `GameManager.Awake` sempre volta a `1f` (padrão da aula). Coberto em Task 8, com hit-stop em tempo real (`WaitForSecondsRealtime`).
4. **Combate re-disparado:** um inimigo morto, ou dois inimigos próximos, não podem abrir dois combates. `CombatManager.TryStartCombat` recusa se já houver combate ativo ou se `enemy.IsDead`. Coberto em Task 7 (teste EditMode do guard em `CombatEncounter`).
5. **Gabarito errado:** uma pergunta cuja resposta "correta" não é o que o Python 3.14 imprime de fato. Coberto em Task 4 por `tools/validate_questions.py`, que executa cada trecho.

---

## Pré-requisito (bloqueante): instalar o editor Unity

A pasta `C:\Program Files\Unity\Hub\Editor\6000.3.9f1` só contém `Editor\Data\PlaybackEngines`, sem `Unity.exe`. O editor precisa ser reinstalado pelo Unity Hub: 6000.3 LTS + módulo **Windows Build Support (Mono)**. Depois, confirmar:

```powershell
Test-Path "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
```

Expected: `True`. Se a versão instalada for outra 6000.3.x, ajustar `$UNITY` abaixo.

Variável usada em todos os comandos:

```powershell
$UNITY = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$PROJ  = "C:\projects\jornada-do-dev"
```

Rodar os testes EditMode (sem `-quit`, que é proibido com `-runTests`):

```powershell
& $UNITY -batchmode -projectPath $PROJ -runTests -testPlatform EditMode -testResults "$PROJ\TestResults\editmode.xml" -logFile "$PROJ\Logs\tests.log"; Select-String -Path "$PROJ\TestResults\editmode.xml" -Pattern '<test-run' | Select-Object -First 1
```

Expected no fim de cada task: `result="Passed"` e `failed="0"`.

## Estrutura de arquivos

```
Assets/
  Art/Kenney/            sprites e tiles CC0 (importados como Point, 16 PPU)
  Audio/Kenney/          sfx e jingles CC0
  Resources/Questions.json
  Input/PlayerControls.inputactions   (+ PlayerControls.cs gerado pelo importer)
  Scenes/MainMenu.unity, Junior.unity, Pleno.unity, Senior.unity, Credits.unity   (geradas)
  Scripts/Core/          Health.cs, PythonQuestion.cs, QuestionBank.cs, CombatEncounter.cs, PhaseInfo.cs   (C# puro)
  Scripts/Player/        PlayerController.cs, PlayerCombatAnimator.cs
  Scripts/Enemies/       EnemyController.cs   (máquina de estados)
  Scripts/Combat/        CombatManager.cs, CombatFx.cs, DamageNumber.cs
  Scripts/UI/            TaskPanel.cs, HudController.cs, MenuController.cs, EndScreen.cs
  Scripts/World/         CameraController.cs, BackgroundController.cs, PhaseExit.cs
  Scripts/Game/          GameManager.cs, AudioManager.cs
  Scripts/JornadaDoDev.asmdef
  Editor/                JornadaSceneBuilder.cs, JornadaBuild.cs, PixelArtImporter.cs, JornadaDoDev.Editor.asmdef
  Tests/EditMode/        HealthTests.cs, QuestionBankTests.cs, CombatEncounterTests.cs, QuestionsJsonTests.cs, JornadaDoDev.Tests.asmdef
tools/validate_questions.py
docs/GDD.md, docs/superpowers/plans/...
README.md, CREDITS.md, .gitignore
```

## Mapa aula → funcionalidade (para a apresentação)

| Tópico da aula (repo) | Onde aparece |
|---|---|
| Input System + `PlayerControls` (Aula2, Aula-3, Aula_4) | `PlayerController`, atalhos 1-4 no `TaskPanel` |
| `Rigidbody2D`, ground check, gravidade assimétrica (Aula_4) | `PlayerController` |
| Animator com `Speed`/`VerticalVelocity`/`isGrounded` (Aula_4) | `PlayerController` + controller gerado |
| Câmera `SmoothDamp` (Aula_4) | `CameraController` + shake |
| Fundo com `mainTextureOffset` (Aula-3, Aula_4) | `BackgroundController` |
| Máquina de estados `State`/`Enter`/`Execute`, `Death`, `DestroyEnemy` (NavMesh_StateMachine) | `EnemyController` (Patrol/Chase/Combat/Dead em 2D, sem NavMesh) |
| `MenuController` + `SceneManager` (NavMesh_Quarta) | menus, troca de fase |
| `GameManager.Instancia`, `vida`, `GameOver`, `Time.timeScale` (Testes_Refinamento) | `GameManager` |
| Testes e refinamento (Testes_Refinamento) | testes EditMode + playtest e balanceamento (Task 10) |

---

### Task 1: Projeto Unity v0 + git

**Files:**
- Create: projeto Unity em `C:\projects\jornada-do-dev` (gerado), `.gitignore`, `README.md`, `Packages/manifest.json` (editado)

**Interfaces:**
- Produces: projeto que abre sem erros, com `com.unity.inputsystem`, `com.unity.ugui`, `com.unity.test-framework`, `com.unity.2d.sprite`; `EditorSettings` em modo 2D; Active Input Handling = Input System Package.

- [ ] **Step 1:** Criar o projeto: `& $UNITY -batchmode -createProject $PROJ -quit -logFile "$env:TEMP\create.log"`. Expected: `$PROJ\Assets` e `$PROJ\ProjectSettings\ProjectVersion.txt` existem. Se o Unity recusar a pasta por não estar vazia (por causa de `docs/`), mover `docs` para `%TEMP%`, criar o projeto e devolver `docs`.
- [ ] **Step 2:** Adicionar ao `Packages/manifest.json` os pacotes acima, nas versões que o editor 6000.3 resolver (`"com.unity.inputsystem": "1.x"` conforme o registry do editor). Ajustar `ProjectSettings/ProjectSettings.asset` para `activeInputHandler: 1` e `ProjectSettings/EditorSettings.asset` para `m_DefaultBehaviorMode: 1`.
- [ ] **Step 3:** `.gitignore` padrão Unity (`[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild/`, `[Bb]uilds/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`, `TestResults/`, `*.csproj`, `*.sln`, `.vs/`, `.idea/`). `README.md` com título, descrição de uma linha, seção "Integrantes" (a preencher pelo grupo) e "Como executar".
- [ ] **Step 4:** Abrir em batchmode para importar: `& $UNITY -batchmode -projectPath $PROJ -quit -logFile "$PROJ\Logs\import.log"`. Expected: exit 0, sem `error CS` no log.
- [ ] **Step 5:** `git init -b main`, `git remote add origin https://github.com/IanAmaral12/jornada-do-dev.git`, commit `chore: projeto Unity 2D inicial (v0)`, `git push -u origin main`. Expected: `gh repo view IanAmaral12/jornada-do-dev --json isEmpty` → `false`.

### Task 2: Assets CC0 (arte 8-bit e sons)

**Files:**
- Create: `Assets/Art/Kenney/**`, `Assets/Audio/Kenney/**`, `Assets/Editor/PixelArtImporter.cs`, `Assets/Editor/JornadaDoDev.Editor.asmdef`, `CREDITS.md`

**Interfaces:**
- Produces: sprites 16×16 de herói, mobs, boss, tiles de chão/parede e fundo; sfx `hit`, `hurt`, `select`, `win`, `lose`; jingle de fase. `PixelArtImporter : AssetPostprocessor` força em `Assets/Art/**`: `textureType Sprite`, `spritePixelsPerUnit 16`, `filterMode Point`, `textureCompression Uncompressed`.

- [ ] **Step 1:** **Pedir confirmação ao usuário antes de baixar** (nome, origem, tamanho) dos packs CC0 da kenney.nl: `Tiny Dungeon` (herói, monstros, boss), `Pixel Platformer` (tiles e fundo), `Interface Sounds` e `Music Jingles` (8-bit). Baixar cada zip numa pasta nova e vazia em `%TEMP%`, copiar só os PNG/OGG usados para `Assets/`, incluindo o `License.txt` de cada pack.
- [ ] **Step 2:** Escrever `PixelArtImporter` e reimportar. Expected: no Inspector (ou via `AssetDatabase` em script), os sprites em `Assets/Art` estão com `Point` e 16 PPU.
- [ ] **Step 3:** `CREDITS.md`: "Arte e áudio: Kenney (www.kenney.nl), licença CC0", listando cada pack com a URL. O mesmo texto vai para a cena de Créditos (Task 9).
- [ ] **Step 4:** Commit `feat: assets 8-bit CC0 da Kenney com importador pixel art`.

### Task 3: Regras de jogo puras + testes EditMode

**Files:**
- Create: `Assets/Scripts/Core/{Health,PythonQuestion,QuestionBank,CombatEncounter,PhaseInfo}.cs`, `Assets/Scripts/JornadaDoDev.asmdef`, `Assets/Tests/EditMode/{HealthTests,QuestionBankTests,CombatEncounterTests}.cs`, `Assets/Tests/EditMode/JornadaDoDev.Tests.asmdef`

**Interfaces:**
- Produces:
  - `public enum Tier { Junior, Pleno, Senior }`
  - `public class Health { Health(int max); int Max; int Current; bool IsDead; event Action<int,int> Changed /*current,max*/; void TakeDamage(int amount) /*clampa em 0*/; void Reset(); }`
  - `[Serializable] public class PythonQuestion { string id; string tier; bool boss; string prompt; string code; string[] options; int correctIndex; string explanation; Tier TierValue => Enum.Parse }`
  - `[Serializable] public class QuestionSet { PythonQuestion[] questions; }`
  - `public class QuestionBank { static QuestionBank FromJson(string json, System.Random rng); PythonQuestion Next(Tier tier, bool boss); int Count(Tier tier, bool boss); }`. Busca por `boss` primeiro e cai para o tier inteiro se não houver perguntas de boss. Sem repetição até esgotar; depois reembaralha, e a primeira do novo ciclo não repete a última servida.
  - `public enum AnswerOutcome { Hit, Miss, EnemyDefeated, PlayerDefeated, Ignored }`
  - `public class CombatEncounter { CombatEncounter(Health player, Health enemy, int damageToEnemy, int damageToPlayer); bool IsOver; PythonQuestion Current; void Present(PythonQuestion q); AnswerOutcome Answer(int optionIndex); }`. `Answer` sem pergunta pendente, ou com o combate encerrado, retorna `Ignored`. Depois de responder, `Current = null` até o próximo `Present`.
  - `public static class PhaseInfo { static string SceneFor(Tier t); static Tier? Next(Tier t); }` → `"Junior"`, `"Pleno"`, `"Senior"`; `Next(Senior) == null`.

- [ ] **Step 1: Testes (falhando)**

```csharp
[Test] public void TakeDamage_ClampsAtZero_AndMarksDead() { var h = new Health(5); h.TakeDamage(7); Assert.AreEqual(0, h.Current); Assert.IsTrue(h.IsDead); }
[Test] public void Changed_FiresWithCurrentAndMax() { var h = new Health(5); int c=-1,m=-1; h.Changed += (a,b)=>{c=a;m=b;}; h.TakeDamage(2); Assert.AreEqual((3,5),(c,m)); }
[Test] public void Next_DoesNotRepeatUntilTierExhausted() // 3 perguntas Junior → 3 ids distintos nas 3 primeiras chamadas
[Test] public void Next_WhenTierExhausted_ReshufflesWithoutImmediateRepeat() // 2 perguntas, 50 chamadas, nunca duas iguais seguidas
[Test] public void Next_Boss_PrefersBossQuestions_FallsBackToTier()
[Test] public void FromJson_InvalidCorrectIndex_Throws() // correctIndex fora de options → ArgumentException com o id
[Test] public void Answer_Correct_DamagesEnemy_ReturnsHit()      // enemy 2 → 1, Hit
[Test] public void Answer_Correct_KillingBlow_ReturnsEnemyDefeated()
[Test] public void Answer_Wrong_DamagesPlayer_ReturnsMiss()
[Test] public void Answer_Wrong_LastHeart_ReturnsPlayerDefeated()
[Test] public void Answer_WhenAlreadyResolved_IsIgnored()        // Answer duas vezes sem Present → 2ª = Ignored, vida inalterada
[Test] public void PhaseInfo_NextOfSenior_IsNull()
```

- [ ] **Step 2:** Rodar os testes. Expected: falha de compilação (tipos não existem).
- [ ] **Step 3:** Implementar as classes com as assinaturas acima (`JsonUtility.FromJson<QuestionSet>`; embaralhamento Fisher–Yates com o `System.Random` injetado).
- [ ] **Step 4:** Rodar os testes. Expected: `result="Passed"`, `failed="0"`.
- [ ] **Step 5:** Commit `feat: regras de vida, banco de perguntas e combate com testes`.

### Task 4: Banco de perguntas Python 3.14 + validador

**Files:**
- Create: `Assets/Resources/Questions.json`, `tools/validate_questions.py`, `Assets/Tests/EditMode/QuestionsJsonTests.cs`

**Interfaces:**
- Consumes: `QuestionSet`/`PythonQuestion` (Task 3).
- Produces: 30 perguntas, 10 por tier. Destas, 3 por tier têm `boss: true`. Cada `code` é um programa Python completo e determinístico que imprime exatamente uma linha, e `options[correctIndex]` é igual a essa linha.

Progressão de conteúdo:
- **Junior:** `+ - * /`, precedência, `//`, `%`, `**`, `int`/`float`, `/` sempre devolve float (`print(7/2)`), variáveis e reatribuição, `abs`/`round` (com banker's rounding: `round(2.5)`). Boss: expressão composta com precedência e `divmod`.
- **Pleno:** strings (fatiamento, `len`, `upper`, f-string), listas (`append`, índice negativo, `sum`), `if/elif/else`, `for` com `range`, `while` com contador, `in`. Boss: laço acumulando com condição (FizzBuzz curto, soma de pares).
- **Senior:** `def` com `return`, parâmetro padrão, retorno múltiplo e desempacotamento, escopo (variável local × global), recursão (fatorial, fibonacci), `lambda` com `sorted(key=...)`, list comprehension com filtro, armadilha do default mutável. Boss: recursão + comprehension, ou closure.

- [ ] **Step 1:** Escrever `validate_questions.py`. Ele lê o JSON e exige `sys.version_info >= (3, 14)`. Para cada pergunta, roda `code` com `subprocess.run([sys.executable, "-I", "-c", code], capture_output=True, text=True, timeout=5)`. Falha se `stdout.strip() != options[correctIndex]`, se as opções tiverem duplicatas, se `len(options) != 4`, ou se algum tier tiver menos de 10 perguntas ou menos de 3 de boss. Imprime `OK 30 perguntas` e sai com 0; senão lista os ids com problema e sai com 1.
- [ ] **Step 2:** Rodar com JSON vazio (`{"questions":[]}`): `py -3.14 tools/validate_questions.py Assets/Resources/Questions.json`. Expected: exit 1 ("Junior: 0/10").
- [ ] **Step 3:** Escrever as 30 perguntas (prompt em PT-BR, p.ex. "O que este código imprime?"). Distratores plausíveis: erro comum de precedência, `3.5` × `3`, off-by-one.
- [ ] **Step 4:** Rodar o validador. Expected: `OK 30 perguntas`, exit 0.
- [ ] **Step 5:** `QuestionsJsonTests.ShippedJson_LoadsAndHasTenPerTier()` carrega `Resources.Load<TextAsset>("Questions")` via `QuestionBank.FromJson` e verifica `Count(t,false) >= 10` e `Count(t,true) >= 3` para cada tier. Rodar os testes Unity. Expected: Passed.
- [ ] **Step 6:** Commit `feat: 30 tasks de Python 3.14 validadas por script`.

### Task 5: Player, input, câmera e fundo (tópicos das aulas)

**Files:**
- Create: `Assets/Input/PlayerControls.inputactions` (com `generateWrapperCode: 1` no `.meta` → `PlayerControls.cs`), `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/World/{CameraController,BackgroundController}.cs`

**Interfaces:**
- Produces:
  - Action maps: `Movement` (`Move` Vector2: WASD/setas; `Jump`: Espaço/W) e `Combat` (`Answer1..Answer4`: teclas 1-4 e numpad).
  - `PlayerController : MonoBehaviour`: campos `moveSpeed=6`, `jumpSpeed=12`, `groundCheck`, `groundCheckRadius=0.1`, `groundLayer`; `public bool InputLocked { get; set; }` (zera a velocidade X e ignora o pulo quando `true`); `public void FaceTowards(float x)`. Mesmo padrão da Aula_4: `OnEnable`/`OnDisable` assinando os eventos, `OverlapCircle`, `flipX`, gravidade 6/2, Animator `Speed`, `VerticalVelocity`, `isGrounded`.
  - `CameraController`: `SmoothDamp` com `smoothTime=0.2`, limites `minX/maxX` do salão, e `public void Shake(float duration, float magnitude)` somando um offset aleatório em tempo real.
  - `BackgroundController`: igual à aula (`mainTextureOffset`), só no eixo X, com velocidade proporcional ao deslocamento da câmera (parallax).

- [ ] **Step 1:** Criar o `.inputactions` e confirmar que o import gera `PlayerControls.cs` (batchmode, `-quit`). Expected: arquivo gerado e log sem `error CS`.
- [ ] **Step 2:** Implementar os três scripts.
- [ ] **Step 3:** Verificação manual na Task 9, quando a cena existir: andar, pular, a câmera segue, o fundo desliza. Nesta task, só compilação limpa e testes verdes.
- [ ] **Step 4:** Commit `feat: player com Input System, câmera suave e fundo com parallax`.

### Task 6: Inimigos com máquina de estados

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyController.cs`

**Interfaces:**
- Consumes: `Health` (Task 3). Esta task cria `Assets/Scripts/Combat/CombatManager.cs` só com o esqueleto (`static Instancia`, `bool InCombat`, `bool TryStartCombat(EnemyController e) => false`), que a Task 7 completa.
- Produces: `EnemyController : MonoBehaviour` com campos `displayName`, `isBoss`, `maxHp` (mob 2, boss 3), `damageToPlayer` (1; boss Senior 2), `patrolSpeed=1`, `chaseSpeed=2.5`, `detectionRange=5`, `combatRange=1.5`, `pointA`, `pointB`. Expõe `public Health Health`, `public bool IsDead`, `public void EnterCombat()`, `public void ExitCombat()`, `public void Death()` (trigger `Death`, colliders off, `Destroy(gameObject, 1.5f)`). Estados internos `abstract class State { Enter(); Execute(); }`: `PatrolState`, `ChaseState`, `CombatState` (parado, olhando para o player) e `DeadState`, no estilo do `EnemyController` da aula, com movimento por `Rigidbody2D.linearVelocity` no eixo X, sem NavMesh. `OnDrawGizmosSelected` mostra os raios.

- [ ] **Step 1:** Implementar. O Chase, ao entrar em `combatRange`, chama `CombatManager.Instancia.TryStartCombat(this)`; se retornar `true`, troca para `CombatState`.
- [ ] **Step 2:** Compilar com os testes. Expected: Passed.
- [ ] **Step 3:** Commit `feat: inimigos 2D com estados patrulha, perseguição, combate e morte`.

### Task 7: Combate + painel da task

**Files:**
- Create: `Assets/Scripts/UI/TaskPanel.cs`, `Assets/Scripts/Game/GameManager.cs` (esqueleto)
- Modify: `Assets/Scripts/Combat/CombatManager.cs` (completar o esqueleto da Task 6)
- Modify: `Assets/Tests/EditMode/CombatEncounterTests.cs` (guard)

**Interfaces:**
- Consumes: `QuestionBank`, `CombatEncounter`, `AnswerOutcome`, `Health` (Task 3); `PlayerController.InputLocked`/`FaceTowards` (Task 5); `EnemyController` (Task 6); `CombatFx` (Task 8). Ordem de execução: **fazer a Task 8 antes da Task 7**, porque o código da 7 chama a 8. Esta task cria `Assets/Scripts/Game/GameManager.cs` só com o esqueleto (`Instancia`, `CurrentTier`, `PlayerHealth`, `Bank`, `OnPlayerDefeated()` e `OnBossDefeated()` com `Debug.Log`), que a Task 9 completa.
- Produces:
  - `CombatManager : MonoBehaviour` com `public static CombatManager Instancia`, `public bool InCombat`, `public bool TryStartCombat(EnemyController e)` (retorna `false` se `InCombat || e.IsDead`). O fluxo é uma corrotina: trava o input → posiciona os dois → `Present(bank.Next(tier, e.isBoss))` → `TaskPanel.Show(q)` → aguarda a resposta → `TaskPanel.Lock()` → toca `CombatFx.PlayerStrike` ou `EnemyStrike` (corrotina) → mostra a explicação por 1,5 s em caso de erro → repete até `IsOver` → em `EnemyDefeated`, `e.Death()` e libera o input; se for boss, chama `GameManager.OnBossDefeated()`; em `PlayerDefeated`, chama `GameManager.OnPlayerDefeated()`.
  - `TaskPanel : MonoBehaviour`: `void Show(PythonQuestion q)` (título com o nome do inimigo, `code` em fonte monoespaçada com realce simples de palavras-chave Python via rich text do TMP, 4 botões `A`–`D`), `event Action<int> Answered`, `void Lock()`, `void ShowFeedback(bool correct, string explanation)`, `void Hide()`. Aceita clique e teclas 1-4 (`PlayerControls.Combat`). Ignora a entrada enquanto estiver travado.
- [ ] **Step 1:** Teste `Answer_AfterEncounterOver_IsIgnored` (inimigo morto → nova `Present` + `Answer` = `Ignored`). Rodar. Expected: FAIL se `Present` aceitar uma pergunta depois de `IsOver`. Ajustar `CombatEncounter.Present` para não fazer nada quando `IsOver`. Rodar. Expected: PASS.
- [ ] **Step 2:** Implementar `CombatManager` e `TaskPanel`.
- [ ] **Step 3:** Commit `feat: combate por tasks de Python com painel e atalhos 1-4`.

### Task 8: Efeitos de golpe "performáticos"

**Files:**
- Create: `Assets/Scripts/Combat/{CombatFx,DamageNumber}.cs`, `Assets/Scripts/Player/PlayerCombatAnimator.cs`

**Interfaces:**
- Consumes: `CameraController.Shake` (Task 5), `EnemyController` (Task 6), sfx (Task 2). Executar antes da Task 7.
- Produces:
  - `CombatFx : MonoBehaviour`: `IEnumerator PlayerStrike(Transform player, EnemyController enemy, int damage, bool finisher)` e `IEnumerator EnemyStrike(EnemyController enemy, Transform player, int damage)`.
  - `PlayerStrike`: wind-up (squash 0,1 s) → dash até o inimigo (0,12 s, easing out-cubic) → sprite de corte em arco com rastro (`TrailRenderer` + rotação de 160° em 0,08 s) → **hit-stop** de 0,07 s (`Time.timeScale = 0`, `WaitForSecondsRealtime`, volta a 1) → flash branco do inimigo (material com `_FlashAmount`, ou troca de cor por 2 frames) → knockback de 0,3 unidade → partículas quadradas (`ParticleSystem`, textura 1×1 Point, 12 partículas) → `DamageNumber` subindo e sumindo → shake (0,15 s, 0,15) → volta à posição. No `finisher`: hit-stop de 0,15 s, shake de 0,3 e câmera em slow motion (`timeScale` 0,3 por 0,4 s real).
  - `EnemyStrike`: o inimigo avança, o herói pisca vermelho 3 vezes, o HUD treme, toca o sfx `hurt`.
  - `DamageNumber`: `static void Spawn(Vector3 pos, int value, Color color)` (TMP no mundo, sobe 0,8 unidade em 0,6 s com fade).
  - `PlayerCombatAnimator`: dispara as triggers `Attack`/`Hurt` no Animator do herói (frames do pack + squash/stretch por escala).
- [ ] **Step 1:** Implementar. Todas as esperas de efeito usam tempo real. `OnDisable`/`OnDestroy` de `CombatFx` forçam `Time.timeScale = 1f`.
- [ ] **Step 2:** Compilar e rodar os testes. Expected: Passed.
- [ ] **Step 3:** Commit `feat: golpes com dash, hit-stop, flash, partículas, shake e números de dano`.

### Task 9: GameManager, HUD, menus, cenas e áudio

**Files:**
- Create: `Assets/Scripts/Game/AudioManager.cs`, `Assets/Scripts/UI/{HudController,MenuController,EndScreen}.cs`
- Modify: `Assets/Scripts/Game/GameManager.cs` (completar o esqueleto da Task 7)
- Create também:, `Assets/Scripts/World/PhaseExit.cs`, `Assets/Editor/JornadaSceneBuilder.cs`, `Assets/Editor/JornadaBuild.cs`

**Interfaces:**
- Consumes: tudo das Tasks 3–8.
- Produces:
  - `GameManager : MonoBehaviour`: `public static GameManager Instancia`; `[SerializeField] Tier tier`; `public Tier CurrentTier`; `public Health PlayerHealth` (5 corações, cheio a cada fase); `public QuestionBank Bank` (carregado uma vez de `Resources/Questions`, com `System.Random` sem seed); `Awake` define `Instancia` e `Time.timeScale = 1f`; `OnPlayerDefeated()` abre o painel Game Over ("Tentar de novo" recarrega a fase, "Menu" vai para `MainMenu`); `OnBossDefeated()` abre a `PhaseExit` (porta) com jingle; `GoToNextPhase()` usa `PhaseInfo.Next`, e com `null` vai ao painel de Vitória e depois a `Credits`.
  - `HudController`: corações (sprites cheio/vazio), nome da fase ("Fase 1 — Dev Junior"), barra de HP do inimigo durante o combate, inscrita em `Health.Changed`.
  - `MenuController`: `Play()` → `SceneManager.LoadScene("Junior")`, `Credits()`, `Quit()` (`Application.Quit`).
  - `AudioManager`: singleton com `DontDestroyOnLoad`, `Play(string sfxName)`, música/jingle por fase.
  - `JornadaSceneBuilder`: menu `Jornada/Gerar Cenas` e `public static void BuildAll()` (para `-executeMethod`). Gera `MainMenu`, `Junior`, `Pleno`, `Senior` e `Credits` em `Assets/Scenes`. Cada fase é um salão de cerca de 40 unidades com chão e paredes `SpriteRenderer` em modo Tiled + `BoxCollider2D` (layer `Ground`), fundo com `BackgroundController`, player na esquerda, mobs e boss à direita, porta de saída no fim, Canvas com HUD, `TaskPanel`, Game Over e Vitória, `CombatManager`, `CombatFx` e `GameManager` com o `tier` certo. Mobs por fase: Junior 1, Pleno 2, Senior 2. O boss tem escala 2× e nome temático ("Bug do Ponto Flutuante", "Loop Infinito", "Recursão Sem Base"). Também registra as cenas em `EditorBuildSettings` na ordem MainMenu, Junior, Pleno, Senior, Credits, e gera os `AnimatorController` do herói e dos inimigos (`Idle`, `Run`, `Jump`, `Attack`, `Hurt`, `Death`).
  - `JornadaBuild.BuildWindows()`: `BuildPipeline.BuildPlayer` das cenas registradas para `Build/JornadaDoDev.exe`, `StandaloneWindows64`. Falha (exit ≠ 0) se `report.summary.result != Succeeded`.
- [ ] **Step 1:** Implementar os scripts de runtime.
- [ ] **Step 2:** Implementar o `JornadaSceneBuilder` e rodar `& $UNITY -batchmode -projectPath $PROJ -executeMethod JornadaSceneBuilder.BuildAll -quit -logFile "$PROJ\Logs\scenes.log"`. Expected: as 5 cenas existem e o log não tem `error`/`Exception`.
- [ ] **Step 3:** Rodar os testes. Expected: Passed.
- [ ] **Step 4:** Abrir o editor (interativo) e jogar o MainMenu → Junior → Pleno → Senior → Créditos. Checar: movimento, entrada em combate, acerto anima o golpe, erro tira coração, Game Over com 0 corações, "Tentar de novo" volta com `timeScale` 1, porta abre após o boss, Vitória e Créditos com o texto da Kenney.
- [ ] **Step 5:** Commit `feat: fases geradas, HUD, menus, game over, vitória, créditos e áudio`.

### Task 10: Playtest, balanceamento, documentação e build

**Files:**
- Create: `docs/GDD.md`
- Modify: `README.md`, valores serializados no `JornadaSceneBuilder` (balanceamento)

- [ ] **Step 1:** Playtest completo cronometrado. Meta: 8 a 12 minutos para quem acerta cerca de 70%. Ajustar `maxHp`/`damageToPlayer`/corações se Junior for impossível de perder ou Senior ficar frustrante. Regerar as cenas e repetir.
- [ ] **Step 2:** `docs/GDD.md` simplificado (1–2 páginas): conceito, público, loop principal, controles, fases e inimigos, sistema de combate e progressão das tasks, arte/áudio (CC0), tabela "tópicos da aula → funcionalidade".
- [ ] **Step 3:** README: integrantes (o grupo preenche), requisitos (Windows 64-bit; o Python não é necessário para jogar), como executar o build, como abrir no Unity 6000.3, como rodar os testes e o validador das perguntas.
- [ ] **Step 4:** Build: `& $UNITY -batchmode -projectPath $PROJ -executeMethod JornadaBuild.BuildWindows -quit -logFile "$PROJ\Logs\build.log"`. Expected: `Build\JornadaDoDev.exe` existe e abre o menu. Compactar `Build\` em `JornadaDoDev-win64.zip` para a entrega e para um release no GitHub.
- [ ] **Step 5:** Commit `docs: GDD, README e ajustes de balanceamento`, `git tag v1.0`, `git push --follow-tags`. Release no GitHub com o zip **só com a confirmação do usuário** (é publicação).

---

## Resumo da spec (fonte deste plano)

- Unity 2D, arte 8-bit, PC; projeto e repo novos; git desde a v0 em `IanAmaral12/jornada-do-dev`.
- Jogo "Jornada do Dev": 3 fases (Junior, Pleno, Senior). Cada uma é um salão curto com 1–2 mobs e 1 boss.
- Combate = task de programação em Python (última versão, 3.14). Começa no básico (operações matemáticas) e avança até funções. Formato escolhido: ler código e responder.
- Acerto: golpe com animação caprichada. Erro: toma golpe e perde vida. Vida 0: Game Over.
- Vida, mecânicas e bibliotecas visuais a critério da implementação; arte em pack gratuito CC0.
- Usar os materiais e tópicos dos repos das aulas; comentários mínimos, em PT-BR.
- P2: build + projeto + GDD + README; créditos dos assets numa tela do jogo; apresentação de até 15 min mostrando jogo, código e contribuições.
