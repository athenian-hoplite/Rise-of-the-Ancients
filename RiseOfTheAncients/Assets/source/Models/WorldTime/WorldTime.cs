using TMPro;
using UnityEngine;

namespace ROTA.Models
{

public class WorldTime : MonoBehaviour
{
    public TMP_Text dateText;
    public GameObject m_pausedPanel;
    private WorldCalendar m_calendar = new WorldCalendar();
    private WorldDate m_today;
    private float m_timeThreshold = 1;
    private float m_curTickTime = 0;
    private static WorldTime s_instance = null;

    public void SetTimeThreshold(float time)
    {
        m_timeThreshold = time;
    }

    public void TogglePause() 
    {
        this.enabled = ! this.enabled;
        m_pausedPanel.SetActive( ! this.enabled);
        m_curTickTime = 0; // On toggling pause current tick is always reset
    }

    public static WorldDate Future(int days)
    {
        return s_instance.m_calendar.DaysInFuture(s_instance.m_today, days);
    }

    private void ToCalendarStart()
    {
        m_today = m_calendar.GetStart();
        dateText.SetText(m_today.Day + "/" + m_today.Month + "/" + m_today.Year);
    }

    private void Tick()
    {
        m_today = m_calendar.NextDay(m_today);
        dateText.SetText(m_today.Day + "/" + m_today.Month + "/" + m_today.Year);
        m_curTickTime = 0;

        CommandQueue.Tick(m_today);
    }

    private bool IsDayTickDone(float deltaTime)
    {
        m_curTickTime += Time.deltaTime;
        return m_curTickTime >= m_timeThreshold;
    }

    private void OnEnable()
    {
        s_instance = this;
    }

    private void Start() 
    {
        ToCalendarStart();
        TogglePause();
    }

    private void Update() 
    {
        if (IsDayTickDone(Time.deltaTime)) Tick();
    }
    
}

}