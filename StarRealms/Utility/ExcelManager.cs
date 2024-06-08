using Microsoft.Data.Sqlite;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using StarRealms.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;

namespace StarRealms.Utility
{
    internal class ExcelManager
    {
        string FileName;
        string FirstPlayer;

        public ExcelManager() { FileName = ""; FirstPlayer = ""; }
        public ExcelManager(string Path)
        {
            FileName = Path;
            InitStaticExcelData();
            FirstPlayer = "";
        }

        private void InitStaticExcelData()
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(FileName)))
            {
                package.Workbook.Worksheets.Add("Общая статистика");
                package.Workbook.Worksheets.Add("Внутриигровая статистика");

                SetFirstPageStandartText(package.Workbook.Worksheets["Общая статистика"]);
                SetSecondPageStandartText(package.Workbook.Worksheets["Внутриигровая статистика"]);

                // Сохранение изменений в файл
                package.Save();
            }
        }
        private static void SetFirstPageStandartText(ExcelWorksheet worksheet)
        {
            var range = worksheet.Cells["A1:E67"];
            range.Style.WrapText = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Columns[1].Width = 15;
            worksheet.Columns[2, 5].Width = 11;

            List<string> TextToInput = ["Количество игр","Имя игрока", "Количество побед", "Имя игрока", "Количество побед"];
            for (int i = 0; i < TextToInput.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = TextToInput[i];
            }
            range = worksheet.Cells[2,1,2,5];
            range.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));


            TextToInput = ["Всего", "Среднее значение", "Всего", "Среднее значение"];
            for (int i = 0; i < TextToInput.Count; i++)
            {
                worksheet.Cells[3, i + 2].Value = TextToInput[i];
                worksheet.Cells[12, i + 2].Value = TextToInput[i];
                worksheet.Cells[18, i + 2].Value = TextToInput[i];
            }

            TextToInput = ["Параметры","Потрачено валюты", "Куплено карт", "Возможный урон", "Урона по противнику", "Заблокировано урона базами", "Вылечено здоровья", "Разыграно карт"];
            for (int i = 0; i < TextToInput.Count; i++)
            {
                worksheet.Cells[i + 3, 1].Value = TextToInput[i];
            }
            range = worksheet.Cells[4, 2, 10, 5];
            range.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            range = worksheet.Cells[1, 1, 10, 5];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);


            TextToInput = ["↓ Фракции ↓", "Торговая федерация", "Слизь", "Технокульт", "Звёздная империя"];
            for (int i = 0; i < TextToInput.Count; i++)
            {
                worksheet.Cells[i + 12, 1].Value = TextToInput[i];
            }
            worksheet.Cells[13, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(189, 215, 238));
            worksheet.Cells[14, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(198, 224, 180));
            worksheet.Cells[15, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(248, 203, 173));
            worksheet.Cells[16, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(255, 230, 153));

            range = worksheet.Cells[13, 2, 16, 5];
            range.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            range = worksheet.Cells[12, 1, 16, 5];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);

            // Для подсчета разыгранных карт
            worksheet.Cells["A18"].Value = "↓ Карты ↓";

            List<string> Cards = new List<string>();
            using (var connection = new SqliteConnection("Data Source=StarEmps.db"))
            {
                string sql = "select Name from CardNames";
                connection.Open();
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Cards.Add(reader.GetString(reader.GetOrdinal("Name")));
                        }
                    }
                }

                sql = "select Name from Researchers";
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Cards.Add(reader.GetString(reader.GetOrdinal("Name")));
                        }
                    }
                }

                sql = "select Name from StartCards";
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Cards.Add(reader.GetString(reader.GetOrdinal("Name")));
                        }
                    }
                }
                connection.Close();
            }
            if (Cards.Count != 49)
            {
                Console.WriteLine("Вытащил не все карты!");
            }
            else
            {
                for (int i = 0; i < Cards.Count; i++)
                {
                    worksheet.Cells[i + 19, 1].Value = Cards[i];

                    worksheet.Cells[i + 19, 2].Style.Numberformat.Format = "#";
                    worksheet.Cells[i + 19, 3].Style.Numberformat.Format = "0.00";
                    worksheet.Cells[i + 19, 4].Style.Numberformat.Format = "#";
                    worksheet.Cells[i + 19, 5].Style.Numberformat.Format = "0.00";
                }
            }
            range = worksheet.Cells[19, 2, 67, 5];
            range.Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            range = worksheet.Cells[18, 1, 67, 5];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);

            worksheet.Cells[19, 1, 25, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(189, 215, 238));
            worksheet.Cells[26, 1, 30, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(155, 194, 230));

            worksheet.Cells[31, 1, 38, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(198, 224, 180));
            worksheet.Cells[39, 1, 41, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(169, 208, 142));

            worksheet.Cells[42, 1, 48, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(248, 203, 173));
            worksheet.Cells[49, 1, 53, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(244, 176, 132));

            worksheet.Cells[54, 1, 59, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(255, 230, 153));
            worksheet.Cells[60, 1, 64, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(255, 217, 102));

            worksheet.Cells[65, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(191, 191, 191));

            worksheet.Cells[66, 1, 67, 1].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(128, 128, 128));

            // Формулы
            worksheet.Cells[2, 1].Formula = "'Внутриигровая статистика'!D1";
            worksheet.Cells[2, 2].Formula = "'Внутриигровая статистика'!B1";
            worksheet.Cells[2, 3].Formula = "'Внутриигровая статистика'!B2";
            worksheet.Cells[2, 4].Formula = "'Внутриигровая статистика'!B19";
            worksheet.Cells[2, 5].Formula = "'Внутриигровая статистика'!B20";

            Dictionary<int, List<int>> PageLinks = new Dictionary<int, List<int>>();
            for (int i = 0; i < 7; i++)
                PageLinks.Add(i + 4, [i + 6, i + 24]);

            for (int i = 0; i < 4; i++)
                PageLinks.Add(i + 13, [i + 14, i + 32]);

            foreach (var row in PageLinks)
            {
                worksheet.Cells[row.Key, 2].FormulaR1C1 = $"SUM('Внутриигровая статистика'!R{row.Value[0]}C2:R{row.Value[0]}C20)";
                worksheet.Cells[row.Key, 3].FormulaR1C1 = $"Average('Внутриигровая статистика'!R{row.Value[0]}C2:R{row.Value[0]}C20)";
                worksheet.Cells[row.Key, 4].FormulaR1C1 = $"SUM('Внутриигровая статистика'!R{row.Value[1]}C2:R{row.Value[1]}C20)";
                worksheet.Cells[row.Key, 5].FormulaR1C1 = $"Average('Внутриигровая статистика'!R{row.Value[1]}C2:R{row.Value[1]}C20)";

                worksheet.Cells[row.Key, 2].Style.Numberformat.Format = "#";
                worksheet.Cells[row.Key, 3].Style.Numberformat.Format = "0.00";
                worksheet.Cells[row.Key, 4].Style.Numberformat.Format = "#";
                worksheet.Cells[row.Key, 5].Style.Numberformat.Format = "0.00";
            }
        }
        private static void SetSecondPageStandartText(ExcelWorksheet worksheet)
        {
            var range = worksheet.Cells["A1:T35"];
            range.Style.WrapText = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Columns[1].Width = 20;
            worksheet.Columns[2,20].Width = 10.8;

            worksheet.Cells[1, 1].Value = "Имя игрока";
            worksheet.Cells[19, 1].Value = "Имя игрока";
            worksheet.Cells[1, 2].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            worksheet.Cells[19, 2].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));

            worksheet.Cells[2, 1].Value = "Количество побед";
            worksheet.Cells[20, 1].Value = "Количество побед";
            worksheet.Cells[2, 2].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            worksheet.Cells[20, 2].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));

            range = worksheet.Cells[1, 1, 2, 4];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
            range = worksheet.Cells[19, 1, 20, 4];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);

            worksheet.Cells[1, 3, 2, 3].Merge = true;
            worksheet.Cells[19, 3, 20, 3].Merge = true;
            worksheet.Cells[1, 3].Value = "Количество игр";
            worksheet.Cells[19, 3].Value = "Количество игр";
            worksheet.Cells[1, 4, 2, 4].Merge = true;
            worksheet.Cells[19, 4, 20, 4].Merge = true;
            worksheet.Cells[1, 4].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            worksheet.Cells[19, 4].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));

            // Стратегии
            range = worksheet.Cells[1, 6, 3, 15];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
            range = worksheet.Cells[19, 6, 21, 15];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);

            worksheet.Cells[1, 6, 1, 15].Merge = true;
            worksheet.Cells[1, 6].Value = "Стратегия";
            worksheet.Cells[19, 6, 19, 15].Merge = true;
            worksheet.Cells[19, 6].Value = "Стратегия";

            List<string> Parameters = ["Корабли","Базы","Технокульт","Слизь","Торговая федерация","Звёздная империя","Валюта","Урон","Лечение","Агрессия"];
            for (int i = 0; i < Parameters.Count; i++)
            {
                worksheet.Cells[2, i + 6].Value = Parameters[i];
                worksheet.Cells[20, i + 6].Value = Parameters[i];
            }
            worksheet.Cells[3, 6, 3, 15].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            worksheet.Cells[21, 6, 21, 15].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));

            // Параметры
            for (int i = 1; i < 20; i++)
            {
                worksheet.Cells[5, i + 1].Value = i;
                worksheet.Cells[23, i + 1].Value = i;
            }

            Parameters = ["Ход", "Потрачено валюты", "Куплено карт", "Возможный урон", "Урона по противнику", "Заблокировано урона базами", "Вылечено здоровья", "Разыграно карт", "Раз дожил до хода", "Технокульт", "Слизь", "Торговая федерация", "Звёздная империя"];
            for (int i = 0; i < Parameters.Count; i++)
            {
                worksheet.Cells[i + 5,  1].Value = Parameters[i];
                worksheet.Cells[i + 23, 1].Value = Parameters[i];
            }
            worksheet.Cells[6, 2, 13, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));
            worksheet.Cells[24, 2, 31, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(217, 217, 217));

            worksheet.Cells[6, 2, 17, 20].Style.Numberformat.Format = "0.00";
            worksheet.Cells[13, 2, 13, 20].Style.Numberformat.Format = "#";
            worksheet.Cells[24, 2, 35, 20].Style.Numberformat.Format = "0.00";
            worksheet.Cells[31, 2, 31, 20].Style.Numberformat.Format = "#";

            worksheet.Cells[14, 1, 14, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(189, 215, 238));
            worksheet.Cells[15, 1, 15, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(248, 203, 173));
            worksheet.Cells[16, 1, 16, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(255, 230, 153));
            worksheet.Cells[17, 1, 17, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(198, 224, 180));

            worksheet.Cells[32, 1, 32, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(189, 215, 238));
            worksheet.Cells[33, 1, 33, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(198, 224, 180));
            worksheet.Cells[34, 1, 34, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(248, 203, 173));
            worksheet.Cells[35, 1, 35, 20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(255, 230, 153));

            range = worksheet.Cells[5, 1, 17, 20];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
            range = worksheet.Cells[23, 1, 35, 20];
            foreach (var cell in range)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            range.Style.Border.BorderAround(ExcelBorderStyle.Thick);

            worksheet.Cells[18,1,18,20].Style.Fill.SetBackground(System.Drawing.Color.FromArgb(13, 13, 13));
        }
        
        public void InitGameData(int TotalGames, List<Player> players)
        {
            if (FirstPlayer == "")
                FirstPlayer = players[0].Name;

            using (ExcelPackage package = new ExcelPackage(new FileInfo(FileName)))
            {
                var worksheet = package.Workbook.Worksheets["Внутриигровая статистика"];
                worksheet.Cells[1,4].Value = TotalGames;
                worksheet.Cells[19,4].Value = TotalGames;

                foreach (Player player in players)
                {
                    int PlayerIndex = player.Name == FirstPlayer ? 1 : 19;
                    worksheet.Cells[PlayerIndex, 2].Value = player.Name;
                    List<int> PlayersStrategy = player.GetStrategy();
                    for (int i = 0; i < PlayersStrategy.Count; i++)
                        worksheet.Cells[PlayerIndex + 2, i + 6].Value = PlayersStrategy[i];
                }
                package.Save();
            }
        }

        public void AddPlayerDataToSheets(Player player)
        {
            AddPlayerDataToFirstSheet(player);
            AddPlayerDataToSecondSheet(player);
        }
        private void AddPlayerDataToFirstSheet(Player player)
        {
            // Тут статистика ТОЛЬКО по использованию карт
            int PlayerIndex = player.Name == FirstPlayer ? 2 : 4;

            using (ExcelPackage package = new ExcelPackage(new FileInfo(FileName)))
            {
                var worksheet = package.Workbook.Worksheets["Общая статистика"];

                for (int i = 0; i < player.StaticticHolder.CardsStatistic.Count; i++)
                {
                    double oldValue;
                    string cardName = worksheet.Cells[i + 19, 1].Value.ToString()!;
                    // Всего
                    if (player.StaticticHolder.Turn > 1 && worksheet.Cells[i + 19, PlayerIndex].Value != null)
                    {
                        oldValue = (double)worksheet.Cells[i + 19, PlayerIndex].Value;
                        worksheet.Cells[i + 19, PlayerIndex].Value = oldValue + player.StaticticHolder.CardsStatistic[cardName];
                    }
                    else
                        worksheet.Cells[i + 19, PlayerIndex].Value = player.StaticticHolder.CardsStatistic[cardName];
                    
                    // Среднее
                    if (player.StaticticHolder.Turn > 1 && worksheet.Cells[i + 19, PlayerIndex + 1].Value != null)
                    {
                        oldValue = (double)worksheet.Cells[i + 19, PlayerIndex].Value;
                        int turn = player.StaticticHolder.Turn;
                        worksheet.Cells[i + 19, PlayerIndex + 1].Value = (oldValue * (turn - 1) + player.StaticticHolder.CardsStatistic[cardName]) / turn;
                    }
                    else
                        worksheet.Cells[i + 19, PlayerIndex + 1].Value = player.StaticticHolder.CardsStatistic[cardName];
                }

                package.Save();
            }

        }
        private void AddPlayerDataToSecondSheet(Player player)
        {
            if (FirstPlayer == "")
                FirstPlayer = player.Name;

            int PlayerIndex = player.Name == FirstPlayer ? 6 : 24;

            using (ExcelPackage package = new ExcelPackage(new FileInfo(FileName)))
            {
                var worksheet = package.Workbook.Worksheets["Внутриигровая статистика"];

                int turn = player.StaticticHolder.Turn;
                for (int t = 0; t < turn; t++)
                {
                    double oldValue;
                    List<int> TurnStats = player.StaticticHolder.GetTurnStats(t);
                    for (int stat = 0; stat < TurnStats.Count; stat++)
                    {
                        if(stat == 7)
                        {
                            if (worksheet.Cells[PlayerIndex + stat, t + 2].Value != null)
                            {
                                oldValue = (double)worksheet.Cells[PlayerIndex + stat, t + 2].Value;
                                worksheet.Cells[PlayerIndex + stat, t + 2].Value = oldValue + TurnStats[stat];
                            }
                            else
                                worksheet.Cells[PlayerIndex + stat, t + 2].Value = TurnStats[stat];
                        }
                        else
                        {
                            if (worksheet.Cells[PlayerIndex + stat, t + 2].Value != null)
                            {
                                oldValue = (double)worksheet.Cells[PlayerIndex + stat, t + 2].Value;
                                double newValue = (oldValue * (t + 1) + TurnStats[stat]) / (t + 2);
                                worksheet.Cells[PlayerIndex + stat, t + 2].Value = newValue;
                            }
                            else
                                worksheet.Cells[PlayerIndex + stat, t + 2].Value = (double)TurnStats[stat];
                        }
                    }
                }

                package.Save();
            }
        }

        public void SetWins(Dictionary<string, int> winCounter)
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(FileName)))
            {
                var ws = package.Workbook.Worksheets["Внутриигровая статистика"];
                foreach (var kvp in winCounter)
                {
                    int playerIndex = kvp.Key == FirstPlayer ? 2 : 20;
                    ws.Cells[playerIndex, 2].Value = kvp.Value;
                }
                package.Save();
            }
        }
    }
}
