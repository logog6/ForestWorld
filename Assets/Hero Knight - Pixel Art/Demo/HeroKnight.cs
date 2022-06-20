using UnityEngine;
using System.Collections;

public class HeroKnight : MonoBehaviour {

    [SerializeField] float      m_speed = 4.0f;
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] bool       m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    public Joystick joystick;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroKnight   m_wallSensorR1;
    private Sensor_HeroKnight   m_wallSensorR2;
    private Sensor_HeroKnight   m_wallSensorL1;
    private Sensor_HeroKnight   m_wallSensorL2;
    private bool                m_isWallSliding = false;
    private bool                m_grounded = false;
    private bool                m_rolling = false;
    private int                 m_facingDirection = 1;
    private int                 m_currentAttack = 0;
    private float               m_timeSinceAttack = 0.0f;
    private float               m_delayToIdle = 0.0f;
    private float               m_rollDuration = 8.0f / 14.0f;
    private float               m_rollCurrentTime;


    // inicjalizacja
    void Start ()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
    }

    // Aktualizacja wywoływana jest raz na klatkę
    void Update ()
    {
        // Zwiększa licznik czasu, który kontroluje kombinację ataków
        m_timeSinceAttack += Time.deltaTime;

        // Zwiększa licznik czasu, który sprawdza czas trwania rzutu
        if (m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        // Wyłącza przewijanie, jeśli minutnik wydłuży czas trwania
        if (m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

        //Sprawdźa, czy postać właśnie wylądowała na ziemi
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        //Sprawdźa, czy postać właśnie zaczęła spadać
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // -- Obsługuj input i ruch --
        //float inputX = Input.GetAxis("Horizontal");
        float inputX = joystick.Horizontal;
        float verticalMove = joystick.Vertical;

        // Zamień kierunek postaci w zależności od kierunku spaceru
        if (inputX > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
        }
            
        else if (inputX < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
        }

        // Ruch
       if (!m_rolling )
        { 
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);
           
        }

        //ustawienie prędości w powietrzu
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        // -- Obsługa animacji --
        //zjazd po ścianie
        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        m_animator.SetBool("WallSlide", m_isWallSliding);

        //Smierc
        if (Input.GetKeyDown("e") && !m_rolling)
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
        }
            
        //Rany
        else if (Input.GetKeyDown("q") && !m_rolling)
            m_animator.SetTrigger("Hurt");

        //Atak
        else if(Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        {
            m_currentAttack++;

            // Powrót do pierwszego po trzecim ataku
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Zresetuje kombinację ataku, jeśli czas od ostatniego ataku jest zbyt duży
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Wywołaje jedną z trzech animacji ataku  "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger("Attack" + m_currentAttack);

            // Rester czasu
            m_timeSinceAttack = 0.0f;
        }

        // blok
        else if (Input.GetMouseButtonDown(1) && !m_rolling)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
        }

        else if (Input.GetMouseButtonUp(1))
            m_animator.SetBool("IdleBlock", false);

        // Przewrót
        else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        {
            m_rolling = true;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        }


        //skok
        

        else if (verticalMove >= .5f  && m_grounded && !m_rolling) //Input.GetKeyDown("space")
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }

        //Bieg
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            // Reset czasu
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }

        //stanie
        else
        {
            // Zapobiega migotaniu przejść w czasie stania w miejscu
            m_delayToIdle -= Time.deltaTime;
                if(m_delayToIdle < 0)
                    m_animator.SetInteger("AnimState", 0);
        }
    }

    // Animacje
    // Slajdy animacji
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            // Ustawia prawidłową pozycję odradzania strzałek
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Obraca strzałkę we właściwym kierunku
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
