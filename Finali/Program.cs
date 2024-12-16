using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        
        var staffList = JsonDeserializeWork<Staff>.Deserialize("People.txt");
        var taskList = JsonDeserializeWork<Task>.Deserialize("Tasks.txt");

        if (staffList == null || taskList == null)
        {
            Console.WriteLine("Ошибка загрузки данных.");
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
            var assignee = staffList.FirstOrDefault(s => s.Id == task.Assignee?.Id);
            if (assignee != null)
            {
                assignee.AssignedTasks.Add(task);
                task.Assignee = assignee;
            }
        }

        var reportGenerator = new TaskReportGenerator(staffList, taskList);

       
        reportGenerator.ReportForEmployee(1, "ReportForEmployee.json");
        reportGenerator.ReportTasksCreatedBetween(DateTimeOffset.Now.AddMonths(-1), DateTimeOffset.Now, "TasksCreated.json");
        reportGenerator.ReportTasksByStatus(TaskStatus.InProgress, "TasksByStatus.json");
        reportGenerator.ReportTasksByRisk(RiskLevel.Red, "TasksByRisk.json");
        reportGenerator.ReportSubTasks(2, "SubTasks.json");
    }
}

public class JsonDeserializeWork<T>
{
    public static List<T> Deserialize(string fileName)
    {
        try
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"Файл {fileName} не найден.");
            }

            string jsonString = File.ReadAllText(fileName);
            var deserializedList = JsonSerializer.Deserialize<List<T>>(jsonString);

            if (deserializedList == null)
            {
                throw new InvalidOperationException($"Ошибка десериализации файла {fileName}");
            }

            return deserializedList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return new List<T>();
        }
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

    public void Display()
    {
        Console.WriteLine("Информация о сотруднике:");
        Console.WriteLine($"ID: {Id}");
        Console.WriteLine($"Имя: {Name}");
        Console.WriteLine($"Должность: {Position}");
        Console.WriteLine($"Логин: {Login}");
        Console.WriteLine($"Длина пароля: {Password.Length}");
        Console.WriteLine($"Начальник: {(Supervisor?.Name ?? "Нет начальника")}");

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
    public Staff? Assignee { get; set; }
    public TaskStatus Status { get; set; }
    public RiskLevel Risks { get; set; }
    public List<int> SubTasks { get; set; } = new List<int>();

    public bool IsOverdue()
    {
        return DateTimeOffset.Now > Deadline;
    }
}

public class TaskReportGenerator
{
    private readonly List<Staff> _staffList;
    private readonly List<Task> _taskList;

    public TaskReportGenerator(List<Staff> staffList, List<Task> taskList)
    {
        _staffList = staffList;
        _taskList = taskList;
    }

    private void SaveReportToFile<T>(T report, string fileName)
    {
        if (report == null || (report is IEnumerable<object> enumerable && !enumerable.Any()))
        {
            Console.WriteLine($"Отчет {fileName} пуст, сохранение пропущено.");
            return;
        }

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fileName, json);
        Console.WriteLine($"Отчет сохранен в файл: {fileName}");
    }

    public void ReportForEmployee(int employeeId, string fileName)
    {
        var staff = _staffList.FirstOrDefault(s => s.Id == employeeId);
        if (staff == null)
        {
            Console.WriteLine("Сотрудник не найден.");
            return;
        }

        var report = staff.AssignedTasks.Select(task => new
        {
            TaskTitle = task.Title,
            Deadline = task.Deadline,
            Risk = task.Risks,
            Status = task.Status
        }).ToList();

        SaveReportToFile(report, fileName);
    }

    public void ReportTasksCreatedBetween(DateTimeOffset start, DateTimeOffset end, string fileName)
    {
        var report = _taskList
            .Where(t => t.CreationDate >= start && t.CreationDate <= end)
            .Select(task => new
            {
                TaskTitle = task.Title,
                SubTasksCount = task.SubTasks.Count,
                Status = task.Status
            }).ToList();

        SaveReportToFile(report, fileName);
    }

    public void ReportTasksByStatus(TaskStatus status, string fileName)
    {
        var report = _taskList
            .Where(t => t.Status == status)
            .Select(task => new
            {
                TaskTitle = task.Title,
                AssigneeName = task.Assignee?.Name ?? "Не назначено",
                Deadline = task.Deadline
            }).ToList();

        SaveReportToFile(report, fileName);
    }

    public void ReportTasksByRisk(RiskLevel risk, string fileName)
    {
        var report = _taskList
            .Where(t => t.Risks == risk)
            .Select(task => new
            {
                TaskTitle = task.Title,
                SubTasksCount = task.SubTasks.Count
            }).ToList();

        SaveReportToFile(report, fileName);
    }

    public void ReportSubTasks(int taskId, string fileName)
    {
        var task = _taskList.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
        {
            Console.WriteLine("Задача не найдена.");
            return;
        }

        if (!task.SubTasks.Any())
        {
            Console.WriteLine("У задачи нет подзадач.");
            return;
        }

        var report = task.SubTasks
            .Select(subTaskId => _taskList.FirstOrDefault(t => t.Id == subTaskId))
            .Where(subTask => subTask != null)
            .Select(subTask => new
            {
                TaskTitle = subTask.Title,
                Deadline = subTask.Deadline,
                Status = subTask.Status
            }).ToList();

        SaveReportToFile(report, fileName);
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
