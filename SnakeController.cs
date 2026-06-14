using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    private Vector2 direcao = Vector2.right;
    private List<Transform> segmentos = new List<Transform>();
    
    [Header("Prefab do Corpo")]
    public GameObject segmentoPrefab;

    private int enemyPassosContador = 0;
    private bool mudouDirecaoNesseFrame = false; 
    private Transform containerCorpo; 

    void Start()
    {
        // Garante que a cabeça comece no plano 2D correto
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        
        // Cria um container na hierarquia para organizar os clones do corpo
        GameObject goContainer = new GameObject("Container_Corpo");
        containerCorpo = goContainer.transform;

        segmentos.Clear();
        segmentos.Add(this.transform);

        // Aplica a rotação inicial baseada na direção de partida
        RotacionarCabeca();

        // Inicia o loop de movimentação por tempo fixo
        StartCoroutine(LoopMovimento());
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.estadoAtual != "JOGANDO" || GameManager.Instance.paused) return;

        // Evita que o jogador aperte dois botões no mesmo frame e se atropele
        if (mudouDirecaoNesseFrame) return;

        // Captura as teclas W, A, S, D ou Setas do teclado
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && direcao != Vector2.down)
        {
            direcao = Vector2.up;
            mudouDirecaoNesseFrame = true;
        }
        else if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) && direcao != Vector2.up)
        {
            direcao = Vector2.down;
            mudouDirecaoNesseFrame = true;
        }
        else if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) && direcao != Vector2.right)
        {
            direcao = Vector2.left;
            mudouDirecaoNesseFrame = true;
        }
        else if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) && direcao != Vector2.left)
        {
            direcao = Vector2.right;
            mudouDirecaoNesseFrame = true;
        }
    }

    IEnumerator LoopMovimento()
    {
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.estadoAtual == "JOGANDO" && !GameManager.Instance.paused)
            {
                Mover();
            }
            // Puxa a velocidade atual configurada no GameManager
            float velocidadeAtual = GameManager.Instance != null ? GameManager.Instance.speed : 10f;
            yield return new WaitForSeconds(1f / velocidadeAtual);
        }
    }

    void Mover()
    {
        mudouDirecaoNesseFrame = false;

        // Move os segmentos de trás para a frente, copiando Posição E Rotação do gomo anterior
        for (int i = segmentos.Count - 1; i > 0; i--)
        {
            segmentos[i].position = segmentos[i - 1].position;
            segmentos[i].rotation = segmentos[i - 1].rotation; 
        }

        // Calcula a nova posição da cabeça
        Vector3 novaPosicao = transform.position + new Vector3(direcao.x, direcao.y, 0f);

        // Sistema de atravessar paredes (fazer a cobra reaparecer no lado oposto)
        if (GameManager.Instance != null)
        {
            GameManager gm = GameManager.Instance;
            if (novaPosicao.x < gm.limiteX.x) novaPosicao.x = gm.limiteX.y;
            else if (novaPosicao.x > gm.limiteX.y) novaPosicao.x = gm.limiteX.x;

            if (novaPosicao.y < gm.limiteY.x) novaPosicao.y = gm.limiteY.y;
            else if (novaPosicao.y > gm.limiteY.y) novaPosicao.y = gm.limiteY.x;
        }

        // Move a cabeça de fato
        transform.position = new Vector3(novaPosicao.x, novaPosicao.y, 0f);
        
        // Gira a cabeça para a direção correta do sprite
        RotacionarCabeca();

        // Checa colisões usando o sistema otimizado por distância matemática
        ChecarColisoesPorGrade();

        // Controla o ritmo de passos dos inimigos perseguidores
        enemyPassosContador++;
        if (enemyPassosContador >= 5)
        {
            enemyPassosContador = 0;
            MoverInimigos();
        }
    }

    // Sistema corrigido para sprites desenhados originalmente olhando para BAIXO
    void RotacionarCabeca()
    {
        if (direcao == Vector2.down)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);      // Mantém original (0°)
        else if (direcao == Vector2.up)
            transform.rotation = Quaternion.Euler(0f, 0f, 180f);    // Inverte completamente (180°)
        else if (direcao == Vector2.left)
            transform.rotation = Quaternion.Euler(0f, 0f, -90f);    // Gira para a esquerda (-90°)
        else if (direcao == Vector2.right)
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);     // Gira para a direita (90°)
    }

    void ChecarColisoesPorGrade()
    {
        // Colisão com o próprio corpo (Começa no índice 1 para não colidir com ela mesma)
        for (int i = 1; i < segmentos.Count; i++)
        {
            if (Vector3.Distance(transform.position, segmentos[i].position) < 0.2f)
            {
                GameOver();
                return;
            }
        }

        // Colisão com Inimigos
        if (GameManager.Instance != null)
        {
            foreach (GameObject inimigo in GameManager.Instance.GetInimigos())
            {
                if (inimigo != null && Vector3.Distance(transform.position, inimigo.transform.position) < 0.2f)
                {
                    GameOver();
                    return;
                }
            }
        }

        // Colisão com Comidas normais na cena
        GameObject[] comidasNaCena = GameObject.FindGameObjectsWithTag("Comida");
        foreach (GameObject comida in comidasNaCena)
        {
            if (comida != null && Vector3.Distance(transform.position, comida.transform.position) < 0.6f)
            {
                ProcessarColisao(comida);
                return;
            }
        }

        // Colisão com PowerUps na cena
        GameObject[] powerupsNaCena = GameObject.FindGameObjectsWithTag("PowerUp");
        foreach (GameObject pu in powerupsNaCena)
        {
            if (pu != null && Vector3.Distance(transform.position, pu.transform.position) < 0.6f)
            {
                ProcessarColisao(pu);
                return;
            }
        }
    }

    // Mantido por segurança caso use gatilhos físicos tradicionais (2D Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null) ProcessarColisao(other.gameObject);
    }

    void ProcessarColisao(GameObject objAcertado)
    {
        if (objAcertado == null) return;

        if (objAcertado.CompareTag("Comida"))
        {
            Crescer();
            Destroy(objAcertado); 

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdicionarPontos();
                GameManager.Instance.Invoke("SpawnComida", 0.01f);
            }
        }
        else if (objAcertado.CompareTag("PowerUp"))
        {
            Destroy(objAcertado);

            if (GameManager.Instance != null)
            {
                string tipo = Random.value > 0.5f ? "LENTO_2X" : "RAPIDO_METADE";
                GameManager.Instance.AtivarPowerUp(tipo);
            }
        }
    }

    void Crescer()
    {
        if (segmentoPrefab == null) return;

        // Instancia o novo pedaço do corpo atrelando-o ao organizador
        GameObject novoSegmento = Instantiate(segmentoPrefab, containerCorpo);
        
        // Copia a posição e a rotação exata do último gomo da lista para nascer alinhado
        Vector3 pos = segmentos[segmentos.Count - 1].position;
        Quaternion rot = segmentos[segmentos.Count - 1].rotation;
        
        novoSegmento.transform.position = new Vector3(pos.x, pos.y, 0f);
        novoSegmento.transform.rotation = rot;
        
        segmentos.Add(novoSegmento.transform);
    }

    public void ResetSnake()
    {
        // Limpa os gomos antigos da tela ao reiniciar
        for (int i = 1; i < segmentos.Count; i++)
        {
            if (segmentos[i] != null) Destroy(segmentos[i].gameObject);
        }
        
        segmentos.Clear();
        segmentos.Add(this.transform);
        
        // Reseta posição e rotação padrão
        transform.position = Vector3.zero; 
        direcao = Vector2.right;
        RotacionarCabeca();
    }

    void MoverInimigos()
    {
        if (GameManager.Instance == null) return;
        Vector3 cabecaPos = transform.position;

        foreach (GameObject inimigo in GameManager.Instance.GetInimigos())
        {
            if (inimigo == null) continue;

            Vector3 inimigoPos = inimigo.transform.position;
            float distX = cabecaPos.x - inimigoPos.x;
            float distY = cabecaPos.y - inimigoPos.y;

            if (distX != 0 || distY != 0)
            {
                if (Mathf.Abs(distX) >= Mathf.Abs(distY) && distX != 0)
                {
                    inimigoPos.x += distX > 0 ? 1f : -1f;
                }
                else if (distY != 0)
                {
                    inimigoPos.y += distY > 0 ? 1f : -1f;
                }
                inimigo.transform.position = new Vector3(inimigoPos.x, inimigoPos.y, 0f);
            }
        }
    }

    void GameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FinalizarJogo(); // Corrigido erro de ortografia ortográfica
        }
        ResetSnake();
    }
}