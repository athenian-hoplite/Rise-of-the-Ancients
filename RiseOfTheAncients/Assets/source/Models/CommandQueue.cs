using System;
using System.Collections.Generic;

namespace ROTA.Models
{

public static class CommandQueue
{
    
    private static Dictionary<MapPawn, Action> m_commands = new Dictionary<MapPawn, Action>();
    private static Dictionary<WorldDate, List<Action>> m_dateCommands = new Dictionary<WorldDate, List<Action>>();

    public static void AddCommand(MapPawn pawn, WorldDate date, Action command)
    {
        m_commands[pawn] = command;
        if (m_dateCommands.ContainsKey(date))
        {
            m_dateCommands[date].Add(command);
        }
        else
        {
            List<Action> commands = new List<Action>();
            commands.Add(command);
            m_dateCommands[date] = commands;
        }
    }

    public static void Tick(WorldDate curDate)
    {
        if (m_dateCommands.ContainsKey(curDate))
        {
            List<Action> commands = m_dateCommands[curDate];
            foreach (Action command in commands)
            {
                command();
            }
        }
    }

}

}