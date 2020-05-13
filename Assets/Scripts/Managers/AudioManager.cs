using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource m_bgmOpening;
    public AudioSource m_bgmFighting;
    public AudioSource m_ShootingPlayer;
    public AudioSource m_ShootingPlayerSync;
    public AudioSource m_shootingEnmey;
    public AudioSource m_shootingHit;
    public AudioSource m_explore;
    public AudioSource m_knife;
    public AudioSource m_recharge;

    private void Start()
    {
        MessageDispacher.Instance.Register(AudioMessage.PlayOpeningBgm, msg =>
        {
            if (m_bgmFighting.isPlaying)
            {
                m_bgmFighting.Stop();
            }
            m_bgmOpening.Play();
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayFightingBgm, msg =>
        {
            if (m_bgmOpening.isPlaying)
            {
                m_bgmOpening.Stop();
            }
            m_bgmFighting.Play();
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayPlayerShooting, msg =>
        {
            if (!m_ShootingPlayer.isPlaying)
            {
                m_ShootingPlayer.Play();
            }
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayPlayerSyncShooting, msg =>
        {
            m_ShootingPlayerSync.Play();
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayEnemyShooting, msg =>
        {
            m_shootingEnmey.Play();
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayHit, msg =>
        {
            m_shootingHit.Play();
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayExplode, msg =>
        {
            m_explore.Play();
        });

        MessageDispacher.Instance.Register(AudioMessage.PlayKnife, msg =>
        {
            m_knife.Play();
        });
    }

    private void Update()
    {
    }
}
