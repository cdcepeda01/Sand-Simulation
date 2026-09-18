using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 80;
    public int height = 60;

    // Tiempo entre generaciones/ticks
    public float updateTime = 0.1f;

    [Header("Condiciones iniciales")]

    // Cantidad de filas superiores donde aparecerá arena inicialmente
    public int initialSandRows = 8;

    // Probabilidad de que cada celda inicial contenga arena
    [Range(0f, 1f)]
    public float initialSandProbability = 0.35f;

    [Header("Información")]
    public bool logEachGeneration = true;


    // ==============================
    // VARIABLES QUE CAMBIAN
    // DURANTE LA SIMULACIÓN
    // ==============================

    // Estado actual de la simulación
    private bool[,] grid;

    // Estado de la siguiente generación
    private bool[,] nextGrid;

    private float timer = 0f;

    private bool isPaused = false;

    private int generation = 0;


    // ==============================
    // REPRESENTACIÓN VISUAL
    // ==============================

    private Texture2D texture;

    private Color32[] pixels;

    private static readonly Color32 SandColor =
        new Color32(218, 178, 90, 255);

    private static readonly Color32 EmptyColor =
        new Color32(25, 25, 25, 255);


    // ==============================
    // INICIO
    // ==============================

    void Start()
    {
        // Se crean las dos matrices
        grid = new bool[width, height];
        nextGrid = new bool[width, height];


        // Eventos del InputManager original del proyecto
        InputManager.Instance.OnPause += TogglePause;
        InputManager.Instance.OnRestart += RestartSimulation;
        InputManager.Instance.OnClear += ClearSimulation;
        InputManager.Instance.OnToggleCell += ToggleCellInput;


        BuildTexture();

        // Crear la condición inicial
        CreateInitialSand();

        // Centrar la cámara sobre la grid
        CenterCamera();

        Debug.Log("Simulación Falling Sand iniciada.");
    }


    // ==============================
    // CICLO DE LA SIMULACIÓN
    // ==============================

    void Update()
    {
        if (isPaused)
            return;

        timer += Time.deltaTime;

        if (timer >= updateTime)
        {
            Step();

            UpdateVisuals();

            timer = 0f;
        }
    }


    // ==============================
    // REGLAS DE FALLING SAND
    // ==============================

    void Step()
    {
        // Limpiamos la siguiente generación
        Array.Clear(nextGrid, 0, nextGrid.Length);


        // -------------------------------------------------
        // PASO 1
        // CAÍDA VERTICAL
        // -------------------------------------------------
        //
        // Todas las partículas que puedan caer directamente
        // hacia abajo tienen prioridad.
        //

        for (int y = 1; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y])
                    continue;


                // ¿La celda inmediatamente debajo está vacía?
                if (!grid[x, y - 1])
                {
                    nextGrid[x, y - 1] = true;
                }
            }
        }


        // -------------------------------------------------
        // PASO 2
        // PARTÍCULAS QUE NO PUDIERON CAER VERTICALMENTE
        // -------------------------------------------------

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // No hay arena
                if (!grid[x, y])
                    continue;


                // -----------------------------------------
                // PARTE INFERIOR DE LA GRID
                // -----------------------------------------

                // Si ya llegó al suelo, se queda allí.
                if (y == 0)
                {
                    nextGrid[x, y] = true;
                    continue;
                }


                // -----------------------------------------
                // YA CAYÓ VERTICALMENTE
                // -----------------------------------------

                // Si debajo estaba libre, esta partícula
                // ya fue procesada en el paso anterior.
                if (!grid[x, y - 1])
                {
                    continue;
                }


                // -----------------------------------------
                // MOVIMIENTO DIAGONAL
                // -----------------------------------------

                bool leftFree = IsFreeForMovement(
                    x - 1,
                    y - 1
                );

                bool rightFree = IsFreeForMovement(
                    x + 1,
                    y - 1
                );


                // -----------------------------------------
                // LAS DOS DIAGONALES ESTÁN LIBRES
                // -----------------------------------------

                if (leftFree && rightFree)
                {
                    // Escoger aleatoriamente
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        nextGrid[x - 1, y - 1] = true;
                    }
                    else
                    {
                        nextGrid[x + 1, y - 1] = true;
                    }
                }

                // -----------------------------------------
                // SOLO IZQUIERDA LIBRE
                // -----------------------------------------

                else if (leftFree)
                {
                    nextGrid[x - 1, y - 1] = true;
                }

                // -----------------------------------------
                // SOLO DERECHA LIBRE
                // -----------------------------------------

                else if (rightFree)
                {
                    nextGrid[x + 1, y - 1] = true;
                }

                // -----------------------------------------
                // BLOQUEADA
                // -----------------------------------------

                else
                {
                    // Abajo, izquierda y derecha ocupadas.
                    // La partícula permanece donde está.
                    nextGrid[x, y] = true;
                }
            }
        }


        // -------------------------------------------------
        // CAMBIO DE GENERACIÓN
        // -------------------------------------------------

        bool[,] temp = grid;

        grid = nextGrid;

        nextGrid = temp;


        generation++;


        // Mostrar evolución en consola
        if (logEachGeneration)
        {
            Debug.Log(
                "Generación " + generation +
                " | Partículas de arena: " + CountSand()
            );
        }
    }


    // Comprueba si una posición puede recibir una partícula.
    bool IsFreeForMovement(int x, int y)
    {
        // Fuera de la grid
        if (x < 0 || x >= width)
            return false;

        if (y < 0 || y >= height)
            return false;


        // Debe estar vacía tanto en el estado actual
        // como en el estado que estamos construyendo.
        return !grid[x, y] && !nextGrid[x, y];
    }


    // ==============================
    // CONDICIONES INICIALES
    // ==============================

    void CreateInitialSand()
    {
        Array.Clear(grid, 0, grid.Length);
        Array.Clear(nextGrid, 0, nextGrid.Length);


        int rows = Mathf.Clamp(
            initialSandRows,
            1,
            height
        );


        // La arena aparece solamente
        // en la parte superior de la grid.
        int startY = height - rows;


        for (int y = startY; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[x, y] =
                    UnityEngine.Random.value
                    < initialSandProbability;
            }
        }


        generation = 0;

        UpdateVisuals();
    }


    // ==============================
    // CONTROLES
    // ==============================

    void TogglePause()
    {
        isPaused = !isPaused;

        Debug.Log(
            isPaused
                ? "Simulación pausada"
                : "Simulación reanudada"
        );
    }


    void RestartSimulation()
    {
        Debug.Log("Reiniciando simulación...");

        CreateInitialSand();

        timer = 0f;
    }


    void ClearSimulation()
    {
        Debug.Log("Limpiando simulación...");

        ClearGrid();

        generation = 0;

        timer = 0f;
    }


    public void ClearGrid()
    {
        Array.Clear(grid, 0, grid.Length);

        Array.Clear(nextGrid, 0, nextGrid.Length);

        UpdateVisuals();
    }


    // ==============================
    // MOUSE
    // ==============================

    void ToggleCellInput()
    {
        if (Mouse.current != null)
        {
            HandleMouseClick();
            return;
        }


        Vector3 camPos =
            Camera.main.transform.position;

        ToggleCellAtWorld(camPos);
    }


    void HandleMouseClick()
    {
        Vector3 worldPos =
            Camera.main.ScreenToWorldPoint(
                Mouse.current.position.ReadValue()
            );

        ToggleCellAtWorld(worldPos);
    }


    void ToggleCellAtWorld(Vector3 worldPos)
    {
        int x =
            Mathf.FloorToInt(worldPos.x);

        int y =
            Mathf.FloorToInt(worldPos.y);


        if (x < 0 || x >= width)
            return;

        if (y < 0 || y >= height)
            return;


        // Agregar o quitar arena manualmente.
        grid[x, y] = !grid[x, y];

        UpdateVisuals();
    }


    // ==============================
    // TEXTURA
    // ==============================

    void BuildTexture()
    {
        texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );


        // Evita que Unity difumine las celdas
        texture.filterMode = FilterMode.Point;

        texture.wrapMode = TextureWrapMode.Clamp;


        pixels =
            new Color32[width * height];


        Sprite sprite = Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                width,
                height
            ),
            Vector2.zero,
            1f
        );


        SpriteRenderer renderer =
            GetComponent<SpriteRenderer>();


        if (renderer == null)
        {
            renderer =
                gameObject.AddComponent<SpriteRenderer>();
        }


        renderer.sprite = sprite;
    }


    void UpdateVisuals()
    {
        for (int y = 0; y < height; y++)
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                pixels[row + x] =
                    grid[x, y]
                    ? SandColor
                    : EmptyColor;
            }
        }


        texture.SetPixels32(pixels);

        texture.Apply();
    }


    // ==============================
    // CONTAR ARENA
    // ==============================

    int CountSand()
    {
        int count = 0;


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y])
                {
                    count++;
                }
            }
        }


        return count;
    }


    // ==============================
    // INFORMACION EN PANTALLA
    // ==============================

    void OnGUI()
    {
        GUI.Box(
            new Rect(10, 10, 260, 100),
            "Falling Sand"
        );


        GUI.Label(
            new Rect(20, 35, 230, 20),
            "Generación: " + generation
        );


        GUI.Label(
            new Rect(20, 55, 230, 20),
            "Partículas: " + CountSand()
        );


        GUI.Label(
            new Rect(20, 75, 230, 20),
            isPaused
                ? "Estado: PAUSADO"
                : "Estado: EJECUTANDO"
        );
    }


    // ==============================
    // CÁMARA
    // ==============================

    void CenterCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;


        cam.transform.position =
            new Vector3(
                width / 2f,
                height / 2f,
                cam.transform.position.z
            );


        float aspect =
            Mathf.Max(cam.aspect, 0.1f);


        float verticalSize =
            height / 2f + 1f;


        float horizontalSize =
            width / (2f * aspect) + 1f;


        cam.orthographicSize =
            Mathf.Max(
                verticalSize,
                horizontalSize
            );
    }


    // ==============================
    // LIMPIEZA DE EVENTOS
    // ==============================

    void OnDestroy()
    {
        if (InputManager.Instance == null)
            return;


        InputManager.Instance.OnPause -= TogglePause;
        InputManager.Instance.OnRestart -= RestartSimulation;
        InputManager.Instance.OnClear -= ClearSimulation;
        InputManager.Instance.OnToggleCell -= ToggleCellInput;
    }
}