using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estados do Jogo")]
    public string estadoAtual = "MENU";
    public bool paused = false;

    [Header("Configurações")]
    public string dificuldade = "Normal";
    public float speedConfig = 10f;
    public float speed = 10f;

    [Header("Pontuação e Tempo")]
    public int score = 0;
    public int highScore = 0;
    public float segundosDecorridos = 0f;

    [Header("Prefabs e Spawns")]
    public GameObject comidaPrefab;
    public GameObject powerUpPrefab;
    public GameObject inimigoPrefab;
    public Vector2 limiteX = new Vector2(-20f, 20f);
    public Vector2 limiteY = new Vector2(-15f, 15f);
    public string tipoPowerUpAtivo = "";

    [Header("Interfaces de UI (Otimizado)")]
    public GameObject painelMenuPrincipal; // Caixinha onde vai o seu Menu Principal

    private List<GameObject> inimigosNaCena = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Garante que o jogo comece no estado de Menu e a tela apareça
        estadoAtual = "MENU";
        if (painelMenuPrincipal != null)
        {
            painelMenuPrincipal.SetActive(true);
        }

        // Recupera o High Score salvo no PC do jogador
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    void Update()
    {
        if (estadoAtual == "JOGANDO" && !paused)
        {
            segundosDecorridos += Time.deltaTime;
        }
    }

    // Função que o botão "JOGAR" vai acionar
    public void ClicouBotaoJogar()
    {
        score = 0;
        segundosDecorridos = 0f;
        speed = speedConfig;
        estadoAtual = "JOGANDO";

        // Esconde o menu para o jogador ver o mapa de jogo
        if (painelMenuPrincipal != null)
        {
            painelMenuPrincipal.SetActive(false);
        }

        // Limpa inimigos antigos se houver e spawna a primeira comida
        LimparInimigos();
        SpawnComida();

        // Se quiser testar um inimigo logo de cara, desquente a linha abaixo:
        // SpawnInimigo();
    }

    public void AdicionarPontos()
    {
        score += 10;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        // A cada 30 pontos, nasce um perseguidor para complicar o jogo
        if (score % 30 == 0)
        {
            SpawnInimigo();
        }
    }

    public void SpawnComida()
    {
        if (comidaPrefab == null) return;

        float posX = Mathf.Round(Random.Range(limiteX.x, limiteX.y));
        float posY = Mathf.Round(Random.Range(limiteY.x, limiteY.y));

        Instantiate(comidaPrefab, new Vector3(posX, posY, 0f), Quaternion.identity);
    }

    public void SpawnInimigo()
    {
        if (inimigoPrefab == null) return;

        float posX = Mathf.Round(Random.Range(limiteX.x, limiteX.y));
        float posY = Mathf.Round(Random.Range(limiteY.x, limiteY.y));

        GameObject novoInimigo = Instantiate(inimigoPrefab, new Vector3(posX, posY, 0f), Quaternion.identity);
        inimigosNaCena.Add(novoInimigo);
    }

    public void AtivarPowerUp(string tipo)
    {
        tipoPowerUpAtivo = tipo;
        if (tipo == "LENTO_2X") speed = speedConfig * 0.5f;
        else if (tipo == "RAPIDO_METADE") speed = speedConfig * 1.5f;

        Invoke("DesativarPowerUp", 5f);
    }

    void DesativarPowerUp()
    {
        tipoPowerUpAtivo = "";
        speed = speedConfig;
    }

    public List<GameObject> GetInimigos()
    {
        return inimigosNaCena;
    }

    void LimparInimigos()
    {
        foreach (GameObject inimigo in inimigosNaCena)
        {
            if (inimigo != null) Destroy(inimigo);
        }
        inimigosNaCena.Clear();
    }

    // Chamada pelo SnakeController quando a cobra colide com algo letal
    public void FinalizarJogo()
    {
        estadoAtual = "MENU";
        
        // Traz o menu de volta na tela
        if (painelMenuPrincipal != null)
        {
            painelMenuPrincipal.SetActive(true);
        }

        LimparInimigos();
    }
}