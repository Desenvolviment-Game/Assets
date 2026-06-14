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
    private bool mudouDirecaoNesseFrame = false; // Trava de segurança para inputs rápidos
    private Transform containerCorpo; // Referência para a pasta organizadora

    void Start()
    {
        // Força a cabeça a iniciar no Z correto
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        
        // Cria automaticamente um objeto organizador na Hierarquia para não entulhar a tela
        GameObject goContainer = new GameObject("Container_Corpo");
        containerCorpo = goContainer.transform;

        // Garante que a lista comece limpa e apenas com a própria cabeça
        segmentos.Clear();
        segmentos.Add(this.transform);

        // Inicia o loop de movimento contínuo
        StartCoroutine(LoopMovimento());
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.estadoAtual != "JOGANDO" || GameManager.Instance.paused) return;

        // Se o jogador já mudou a direção e a cobra ainda não deu o passo, bloqueia novos comandos
        if (mudouDirecaoNesseFrame) return;

        // Captura as setas ou WASD sem permitir que ela vire diretamente de costas
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
            // Ajusta o tempo com base na velocidade dinâmica do GameManager
            float velocidadeAtual = GameManager.Instance != null ? GameManager.Instance.speed : 10f;
            yield return new WaitForSeconds(1f / velocidadeAtual);
        }
    }

    void Mover()
    {
        // Como a cobra vai andar agora, liberamos a trava para aceitar o próximo comando do teclado
        mudouDirecaoNesseFrame = false;

        // Move os segmentos do rabo de trás para frente
        for (int i = segmentos.Count - 1; i > 0; i--)
        {
            segmentos[i].position = segmentos[i - 1].position;
        }

        // Calcula a nova posição da cabeça
        Vector3 novaPosicao = transform.position + new Vector3(direcao.x, direcao.y, 0f);

        // Sistema de atravessar paredes (Wrap-around)
        if (GameManager.Instance != null)
        {
            GameManager gm = GameManager.Instance;
            if (novaPosicao.x < gm.limiteX.x) novaPosicao.x = gm.limiteX.y;
            else if (novaPosicao.x > gm.limiteX.y) novaPosicao.x = gm.limiteX.x;

            if (novaPosicao.y < gm.limiteY.x) novaPosicao.y = gm.limiteY.y;
            else if (novaPosicao.y > gm.limiteY.y) novaPosicao.y = gm.limiteY.x;
        }

        transform.position = new Vector3(novaPosicao.x, novaPosicao.y, 0f);

        // Checa colisões logo após se mover
        ChecarColisoesPorGrade();

        // Controla o movimento dos inimigos a cada 5 passos
        enemyPassosContador++;
        if (enemyPassosContador >= 5)
        {
            enemyPassosContador = 0;
            MoverInimigos();
        }
    }

    void ChecarColisoesPorGrade()
    {
        // 1. Colisão com o próprio corpo (ignora o índice 0 que é a própria cabeça)
        for (int i = 1; i < segmentos.Count; i++)
        {
            if (Vector3.Distance(transform.position, segmentos[i].position) < 0.2f)
            {
                Debug.Log("Game Over: Bateu no rabo!");
                GameOver();
                return;
            }
        }

        // 2. Colisão com os Inimigos na cena
        if (GameManager.Instance != null)
        {
            foreach (GameObject inimigo in GameManager.Instance.GetInimigos())
            {
                if (inimigo != null && Vector3.Distance(transform.position, inimigo.transform.position) < 0.2f)
                {
                    Debug.Log("Game Over: Bateu no Inimigo!");
                    GameOver();
                    return;
                }
            }
        }

        // 3. Colisão com a Comida usando busca ativa por Tag
        GameObject[] comidasNaCena = GameObject.FindGameObjectsWithTag("Comida");
        foreach (GameObject comida in comidasNaCena)
        {
            if (comida != null && Vector3.Distance(transform.position, comida.transform.position) < 0.6f)
            {
                ProcessarColisao(comida);
                return; // Interrompe para evitar colisões múltiplas no mesmo frame
            }
        }

        // 4. Colisão com Power-ups usando busca ativa por Tag
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

    // Gatilhos físicos reservas (caso a Unity resolva usar a física padrão)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null) ProcessarColisao(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject != null) ProcessarColisao(collision.gameObject);
    }

    void ProcessarColisao(GameObject objAcertado)
    {
        if (objAcertado == null) return;

        if (objAcertado.CompareTag("Comida"))
        {
            // 1. Primeiro faz a cobra crescer
            Crescer();
            
            // 2. Destrói IMEDIATAMENTE o objeto antigo para limpar o espaço na grade
            Destroy(objAcertado); 

            // 3. Comunica com o GameManager de forma assíncrona/segura
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdicionarPontos();
                
                // Dá um intervalo mínimo para a Unity limpar a memória antes de sortear nova comida
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

        // OTIRMIZAÇÃO: Instancia o novo segmento já definindo o containerCorpo como pai dele
        GameObject novoSegmento = Instantiate(segmentoPrefab, containerCorpo);
        
        // Posição do novo segmento vai atrás do último elemento atual da lista
        Vector3 pos = segmentos[segmentos.Count - 1].position;
        novoSegmento.transform.position = new Vector3(pos.x, pos.y, 0f);
        
        segmentos.Add(novoSegmento.transform);
    }

    public void ResetSnake()
    {
        // Limpa o corpo antigo guardando apenas a cabeça
        for (int i = 1; i < segmentos.Count; i++)
        {
            if (segmentos[i] != null) Destroy(segmentos[i].gameObject);
        }
        
        segmentos.Clear();
        segmentos.Add(this.transform);
        
        transform.position = Vector3.zero; 
        direcao = Vector2.right;
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
            GameManager.Instance.estadoAtual = "MENU";
        }
        ResetSnake();
    }
}