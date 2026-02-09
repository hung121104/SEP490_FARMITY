using UnityEngine;

public interface ITimeManagerService
{
    //add by Nhan
    int GetDay();
    int GetWeek();
    int GetMonth();
    int GetYear();
    Season GetSeason();
    string GetCurrentTimeString();
}
