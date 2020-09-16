using System;
using System.Collections.Generic;

namespace ROTA.Models
{

public class WorldCalendar
{

    Dictionary<int, int> m_monthDays = new Dictionary<int,int>() 
    {
        {1, 31}, {2, 28}, {3, 30}, {4, 31}, {5, 30}, {6, 31}, {7, 30}, {8, 31}, {9, 30}, {10, 31}, {11, 30}, {12, 31}
    };

    public WorldCalendar()
    {
    }

    public WorldCalendar(Dictionary<int, int> monthDays)
    {
        m_monthDays = monthDays;
    }

    public WorldDate GetStart()
    {
        return new WorldDate(0, 1, 1);
    }

    public WorldDate NextDay(WorldDate date)
    {
        return DaysInFuture(date, 1);
    }

    public WorldDate DaysInFuture(WorldDate date, int daysInFuture)
    {
        int daysToGo = daysInFuture;
        int curMonthDays = m_monthDays[date.Month];
        int curMonthLeft = Math.Abs(date.Day - curMonthDays);

        if (daysToGo < curMonthLeft)
        {
            date.Day += daysToGo;
            return date;
        }
        else if (daysToGo == curMonthLeft) return NextMonth(date);

        date = NextMonth(date);
        daysToGo -= curMonthLeft + 1;

        while (daysToGo > 0)
        {
            curMonthDays = m_monthDays[date.Month];
            if (daysToGo < curMonthDays) 
            {
                date.Day += daysToGo;
                return date;
            }
            else if (daysToGo == curMonthDays) return NextMonth(date);

            date = NextMonth(date);
            daysToGo -= curMonthDays + 1;
        }

        return date;
    }

    private WorldDate NextMonth(WorldDate date)
    {
        date.Month++;
        if (date.Month > m_monthDays.Count)
        {
            date.Month = 1;
            date.Year++;
        }
        date.Day = 1;
        return date;
    }

}

}