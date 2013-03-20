/*  Based off the javascript FlightSim.js by Mirage, converted by mglenstevens

This is a 3D First Person Controller script that can be used to simulate anything from 
high speed aerial combat, to submarine warfare. I have tried to make my variable names
as descriptive as possible so you can immediately see what they affect. The variables are
sorted by type; speed with speed, rotation with rotation ect... I have also included generic
ranges on variables that need them; relative upper and lower variable limits. This is because 
some of the variables have a greater effect as they approach 1, while others have greater
impact as they approach infinity. If for some reason the ship is not doing what it is supposed to,
check the ranges, as some variables create problems when they are set to 0 or very large values.
 
Also note the separate script titled Space Flight Script. This script has been
optimized to better suit space combat. The effects of gravity, drag and lift are removed
to better simulate flight in zero-gravity space.
 
This script uses controls based off 4 axis. I found these parameters worked well...
    Name : (Roll, Pitch, Yaw or Throttle)
    Gravity : 20
    Dead : 0.001
    Sensitivity : 1
 
Axis for each control (Axis based off a standard flight joystick).
    Pitch: Y- Axis
    Roll: X - Axis
    Yaw: 3'rd Axis
    Throttle: 4'th - Axis
 
How to use this script: 
 
    Drag and Drop the Transform and its Rigidbody onto the variables Flyer and
    FlyerRigidbody in the inspector panel. Remember to change the rigidbody's Drag
    value. If you dont change this, gravity will be unrealistic... (I set drag to 500)
 
    Change the variables to simulate the flight style you desire.
 
    Create a prefab of your GameObject, and back it up to a secure location.
 
    *Note: This is important because none of the variables are stored in the 
    script. If for some reason Unity crashes during testing, the variables
    are not stored when you save the javascript, but when you save the game
    project.
 
    Save often and enjoy!
 
    ~Mirage~
*/

using UnityEngine;
using System.Net;

public class FlightController : MonoBehaviour
{
    // Singleton access
    //static FlightController _instance;
    //public static FlightController Instance { get { return _instance; } }

    // Components
    public Transform flyer;
    public Rigidbody flyerRigidbody;
    public Transform seaLevelTransform;

    // Assorted control variables. These mostly handle realism settings, change as you see fit.
    public float accelerateConst = 5;          // Set these close to 0 to smooth out acceleration. Don't set it TO zero or you will have a division by zero error.
    public float decelerateConst = 0.065f;    // I found this value gives semi-realistic deceleration, change as you see fit.

    /* The ratio of MaxSpeed to Speed Const determines your true max speed. The formula is maxSpeed/SpeedConst = True Speed. 
        This way you wont have to scale your objects to make them seem like they are going fast or slow.
        MaxSpeed is what you will want to use for a GUI though.
    */
    public static float maxSpeed = 100;
    public float speedConst = 50;

    public int throttleConst = 50;
    public float raiseFlapRate = 1;               // Smoother when close to zero
    public float lowerFlapRate = 1;               // Smoother when close to zero
    public float maxAfterburner = 5;          // The maximum thrust your afterburner will produce
    public float afterburnerAccelerate = 0.5f;
    public float afterburnerDecelerate = 1;
    public float liftConst = 7.5f;             // Another arbitrary constant, change it as you see fit.
    public float angleOfAttack = 15;         // Effective range: 0 <= angleOfAttack <= 20
    public float gravityConst = 9.8f;          // An arbitrary gravity constant, there is no particular reason it has to be 9.8...
    public int levelFlightPercent = 25;
    public float maxDiveForce = 0.1f;
    public float noseDiveConst = 0.01f;
    public float minSmooth = 0.5f;
    public float maxSmooth = 500;
    public float maxControlSpeedPercent = 75;     // When your speed is withen the range defined by these two variables, your ship's rotation sensitivity fluxuates.
    public float minControlSpeedPercent = 25;     // If you reach the speed defined by either of these, your ship has reached it's max or min sensitivity.


    // Rotation Variables, change these to give the effect of flying anything from a cargo plane to a fighter jet.
    public bool lockRotation;       // If this is checked, it locks pitch roll and yaw constants to the var rotationConst.
    public int lockedRotationValue = 120;
    public int pitchConst = 100;
    public int rollConst = 100;
    public int yawConst = 100;
    public int ceilingMin = 1000;
    public int ceilingMax = 2000;

    // Airplane Aerodynamics - I strongly reccomend not touching these...
    private float nosePitch;
    private float trueSmooth;
    private float smoothRotation;
    private float truePitch;
    private float trueRoll;
    private float trueYaw;
    private float trueThrust;
    public static float trueDrag;

    // Misc. Variables
    public static float afterburnerConst;
    public static float altitude;


    // HUD and Heading Variables. Use these to create your insturments.
    public static float trueSpeed;
    public static float attitude;
    public static float incidence;
    public static float bank;
    public static float heading;

    public string ipString = "127.0.0.1";

    float sentPitch = 0;
    float sentYaw = 0;
    float sentRoll = 0;


    // Let the games begin!
    void Start()
    {
        trueDrag = 0;
        afterburnerConst = 0;
        smoothRotation = minSmooth + 0.01f;
        if (lockRotation == true)
        {
            pitchConst = lockedRotationValue;
            rollConst = lockedRotationValue;
            yawConst = lockedRotationValue;
            Screen.showCursor = false;
        }
        ipString = GetIP();
    }


    void Update()
    {

        // * * This section of code handles the plane's rotation.

        float pitch = -Input.GetAxis("Pitch") * pitchConst;
        float roll = Input.GetAxis("Roll") * rollConst;
        float yaw = -Input.GetAxis("Yaw") * yawConst;

        pitch *= Time.deltaTime;
        roll *= -Time.deltaTime;
        yaw *= Time.deltaTime;

        // Smothing Rotations...   
        if ((smoothRotation > minSmooth) && (smoothRotation < maxSmooth))
        {
            smoothRotation = Mathf.Lerp(smoothRotation, trueThrust, (maxSpeed - (maxSpeed / minControlSpeedPercent)) * Time.deltaTime);
        }
        if (smoothRotation <= minSmooth)
        {
            smoothRotation = smoothRotation + 0.01f;
        }
        if ((smoothRotation >= maxSmooth) && (trueThrust < (maxSpeed * (minControlSpeedPercent / 100))))
        {
            smoothRotation = smoothRotation - 0.1f;
        }
        trueSmooth = Mathf.Lerp(trueSmooth, smoothRotation, 5 * Time.deltaTime);
        truePitch = Mathf.Lerp(truePitch, pitch, trueSmooth * Time.deltaTime);
        trueRoll = Mathf.Lerp(trueRoll, roll, trueSmooth * Time.deltaTime);
        trueYaw = Mathf.Lerp(trueYaw, yaw, trueSmooth * Time.deltaTime);




        // * * This next block handles the thrust and drag.
        float throttle = (((-(Input.GetAxis("Throttle"))) / 2f) * 90);

        if (transform.position.y > ceilingMin)
        {
            //Find the percentage of thinning atmosphere
            float airThinning = ((transform.position.y - ceilingMin) / (ceilingMax - ceilingMin)) / 20f;
            //Apply the thin atmosphere to the throttle
            throttle = throttle * airThinning;
        }

        if (throttle / speedConst >= trueThrust)
        {
            trueThrust = Mathf.SmoothStep(trueThrust, throttle / speedConst, accelerateConst * Time.deltaTime);
        }
        if (throttle / speedConst < trueThrust)
        {
            trueThrust = Mathf.Lerp(trueThrust, throttle / speedConst, decelerateConst * Time.deltaTime);
        }

        rigidbody.drag = liftConst * ((trueThrust) * (trueThrust));

        if (trueThrust <= (maxSpeed / levelFlightPercent))
        {

            nosePitch = Mathf.Lerp(nosePitch, maxDiveForce, noseDiveConst * Time.deltaTime);
        }
        else
        {

            nosePitch = Mathf.Lerp(nosePitch, 0, 2 * noseDiveConst * Time.deltaTime);
        }

        trueSpeed = ((trueThrust / 2f) * maxSpeed);

        // ** Additional Input

        // Airbrake
        /*  if (Input.GetButton ("Airbrake"))
            {
               trueDrag = Mathf.Lerp (trueDrag, trueSpeed, raiseFlapRate * Time.deltaTime);   
     
            }
     
            if ((!Input.GetButton ("Airbrake"))&&(trueDrag !=0))
            {
               trueDrag = Mathf.Lerp (trueDrag, 0, lowerFlapRate * Time.deltaTime);
            }
     
     
            // Afterburner
            if (Input.GetButton ("Afterburner"))
            {
               afterburnerConst = Mathf.Lerp (afterburnerConst, maxAfterburner, afterburnerAccelerate * Time.deltaTime);     
            }
     
            if ((!Input.GetButton ("Afterburner"))&&(afterburnerConst !=0))
            {
               afterburnerConst = Mathf.Lerp (afterburnerConst, 0, afterburnerDecelerate * Time.deltaTime);
            }
     
        */
        // Adding nose dive when speed gets below a percent of your max speed  
        if (((trueSpeed - trueDrag) + afterburnerConst) <= (maxSpeed * levelFlightPercent / 100))
        {
            noseDiveConst = Mathf.Lerp(noseDiveConst, maxDiveForce, (((trueSpeed - trueDrag) + afterburnerConst) - (maxSpeed * levelFlightPercent / 100)) * 5 * Time.deltaTime);
            flyer.Rotate(noseDiveConst, 0, 0, Space.World);
        }


        // Calculating Flight Mechanics. Used mostly for the HUD.
        attitude = -((Vector3.Angle(Vector3.up, flyer.forward)) - 90);
        bank = ((Vector3.Angle(Vector3.up, flyer.up)));
        incidence = attitude + angleOfAttack;
        heading = flyer.eulerAngles.y;

        if (seaLevelTransform != null)
        {
            altitude = (flyer.transform.position.y - seaLevelTransform.transform.position.y);
        }
        //Debug.Log ((((trueSpeed - trueDrag) + afterburnerConst) - (maxSpeed * levelFlightPercent/100)));

    }   // End function Update( );


    void FixedUpdate()
    {
        if (trueThrust <= maxSpeed)
        {
            // Horizontal Force
            transform.Translate(0, 0, ((trueSpeed - trueDrag) / 100 + afterburnerConst));
        }

        flyerRigidbody.AddForce(0, (rigidbody.drag - gravityConst), 0);
        transform.Rotate(truePitch, -trueYaw, trueRoll);

        //Server.airSpeed = trueSpeed;
        //Server.altitude = altitude;
        sentPitch = transform.localEulerAngles.x;
        sentYaw = transform.localEulerAngles.z;
        sentRoll = transform.localEulerAngles.y;
        //Server.pitch = sentPitch;
        //Server.yaw = sentYaw;
        //Server.roll = sentRoll;


    }// End function FixedUpdateUpdate( )



    void OnGUI()
    {
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical("Box");
        GUILayout.Label("IP");
        GUILayout.Label(ipString);
        GUILayout.Label("Airspeed");
        GUILayout.Label(trueSpeed.ToString());
        GUILayout.Label("Altitude");
        GUILayout.Label(altitude.ToString());
        GUILayout.Label("Pitch");
        GUILayout.Label(sentPitch.ToString());
        GUILayout.Label("Roll");
        GUILayout.Label(sentRoll.ToString());
        GUILayout.Label("Yaw");
        GUILayout.Label(sentYaw.ToString());
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
    }

    string GetIP()
    {
        string strHostName = "";
        strHostName = System.Net.Dns.GetHostName();

        IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(strHostName);

        IPAddress[] addr = ipEntry.AddressList;

        return addr[addr.Length - 1].ToString();

    }
    //Rigidbody this_Rigidbody;

    //public Vector3 flightForce;
    //public float liftForce;

    //// Use this for initialization
    //void Start () {
    //    _instance = this;
    //    this_Rigidbody = rigidbody;
    //}

    //// Update is called once per frame
    //void Update () {
    //    this_Rigidbody.AddForce(0, liftForce, 0);
    //}

    //public void Throw()
    //{
    //    flyerRigidbody.isKinematic = false;
    //    //flyerRigidbody.AddForce(flightForce, ForceMode.Impulse);
    //}

    //public void OnCollisionEnter(Collision col)
    //{
    //    flyerRigidbody.velocity = Vector3.zero;
    //    flyerRigidbody.isKinematic = true;
    //}
}
