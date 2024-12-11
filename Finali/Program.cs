using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        const string jsonStaff = "People.txt";
        const string jsonTask = "Tasks.txt";

        
        if (!File.Exists(jsonStaff) || !File.Exists(jsonTask))
        {
            Console.WriteLine("Файлы People.txt или Tasks.txt отсутствуют.");
            return;
        }

        
        var staffList = JsonDeserializeWork<Staff>.Deserialize(jsonStaff);
        var taskList = JsonDeserializeWork<Task>.Deserialize(jsonTask);

        if (staffList == null || taskList == null)
        {
            Console.WriteLine("Ошибка: данные не были загружены.");
            return;
        }

        
        foreach (var staff in staffList)
        {
            if (staff.SupervisorId.HasValue)
            {
                staff.Supervisor = staffList.FirstOrDefault(s => s.Id == staff.SupervisorId.Value);
            }
        }

        
        foreach (var task in taskList)
        {
            var assignee = staffList.FirstOrDefault(s => s.Id == task.Assignee.Id);
            if (assignee != null)
            {
                assignee.AssignedTasks.Add(task);
                task.Assignee = assignee;
            }
        }

        
        foreach (var staff in staffList)
        {
            staff.Display();
        }
    }
}

public class JsonDeserializeWork<T>
{
    public static List<T> Deserialize(string json)
    {
        if (!File.Exists(json))
        {
            throw new FileNotFoundException($"Файл {json} не найден.");
        }

        string jsonString = File.ReadAllText(json);
        var deserializedList = JsonSerializer.Deserialize<List<T>>(jsonString);

        if (deserializedList == null)
        {
            throw new InvalidOperationException($"Ошибка десериализации файла {json}");
        }

        return deserializedList;
    }
}

public class Staff
{
    public int Id { get; set; }
    public string Name { get; set; }
    public JobPosition Position { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public int? SupervisorId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Staff? Supervisor { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public List<Task> AssignedTasks { get; set; } = new List<Task>();

    private string GetSupervisorInfo()
    {
        return Supervisor == null ? "Нет начальника" : Supervisor.Name ?? "Имя начальника не указано";
    }

    public void Display()
    {
        Console.WriteLine("Информация о сотруднике:");
        Console.WriteLine($"ID: {Id}");
        Console.WriteLine($"Имя: {Name}");
        Console.WriteLine($"Должность: {Position}");
        Console.WriteLine($"Логин: {Login}");
        Console.WriteLine($"Длина пароля: {Password.Length}");
        Console.WriteLine($"Начальник: {GetSupervisorInfo()}");

        if (AssignedTasks.Count > 0)
        {
            Console.WriteLine("Задачи:");
            foreach (var task in AssignedTasks)
            {
                Console.WriteLine($"- {task.Title} (Срок: {task.Deadline}, Статус: {task.Status}, Риски: {task.Risks})");
            }
        }
        else
        {
            Console.WriteLine("Нет назначенных задач.");
        }

        Console.WriteLine();
    }
}

public class Task
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public Staff Assignee { get; set; }
    public TaskStatus Status { get; set; }
    public RiskLevel Risks { get; set; }
    public List<int> SubTasks { get; set; } = new List<int>();

    public bool IsOverdue()
    {
        return DateTimeOffset.Now > Deadline;
    }
}

public enum RiskLevel
{
    Gray,
    Green,
    Yellow,
    Red
}

public enum TaskStatus
{
    Planned,
    InProgress,
    InReview,
    Closed
}

public enum JobPosition
{
    FrontendDeveloper,
    BackendDeveloper,
    Analyst,
    TeamLead,
    Accountant,
    ScrumMaster,
    Administrator
}
