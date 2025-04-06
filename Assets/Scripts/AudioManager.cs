using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonSound;
    [SerializeField] private AudioClip laserSound;
    [SerializeField] private AudioClip explosionSound;

    private AudioSource _sfxSource;

    private static AudioManager instance;
    public static AudioManager Instance
    {
        get { return instance; }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            _sfxSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButtonSound()
    {
        _sfxSource.PlayOneShot(Instance.buttonSound);
    }

    public void PlayLaserSound()
    {
        _sfxSource.PlayOneShot(Instance.laserSound);
    }

    public void PlayExplosionSound()
    {
        _sfxSource.PlayOneShot(Instance.explosionSound);
    }
} 