using UnityEngine;

public class KillParticle : MonoBehaviour 
{
    private ParticleSystem ps;
    public float lifespan;
    private float startTime;

    void Start () 
    {
        ps = GetComponent<ParticleSystem>();
        startTime = Time.time;
    }
    
    void Update () 
    {
        if ((Time.time - startTime) > lifespan || (ps && !ps.IsAlive()))
        {
            Destroy(gameObject);
        }
    }
}
