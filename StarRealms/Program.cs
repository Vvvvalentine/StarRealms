// See https://aka.ms/new-console-template for more information


using OfficeOpenXml;
using StarRealms.Game;
using StarRealms.Utility;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

List<StaticDecisionMaker> decisionMakers = new List<StaticDecisionMaker>();
// "Мозг" для одного игрока
StaticDecisionMaker firstPlayerDM = new StaticDecisionMaker(ShPr: 1,
                                                            BPr: 1,
                                                            Rp: 100,
                                                            Gp: 1,
                                                            Bp: 1,
                                                            Yp: 1,
                                                            G: 100,
                                                            D: 100,
                                                            H: 100,
                                                            Agr: 20);
// "Мозг" для другого игрока
StaticDecisionMaker secondPlayerDM = new StaticDecisionMaker(ShPr: 1,
                                                            BPr: 10,
                                                            Rp: 1,
                                                            Gp: 100,
                                                            Bp: 1,
                                                            Yp: 100,
                                                            G: 100,
                                                            D: 100,
                                                            H: 100,
                                                            Agr: 20);

// Эталон
/*
(ShPr: 1,
                                                            BPr: 1,
                                                            Rp: 100,
                                                            Gp: 100,
                                                            Bp: 100,
                                                            Yp: 100,e
                                                            G: 100,
                                                            D: 100,
                                                            H: 100,
                                                            Agr: 20);
*/

//Game game = new Game(2);
string Path = "../../../../../Results/";                Path += "R vs B_GY";
Path += ".xlsx";
Game game = new Game(Path, firstPlayerDM, secondPlayerDM);
Dictionary<string, int> winCounter = new Dictionary<string, int>();

int TotalGames = 2000;
game.excelManager.InitGameData(TotalGames, game.Players);

for (int i = 0; i < TotalGames; i++)
{
    string winner = game.StartGame();
    if (winCounter.TryGetValue(winner, out int val))
        winCounter[winner]++;
    else winCounter.Add(winner, 1);

    foreach(Player player in game.Players)
        game.excelManager.AddPlayerDataToSheets(player);
}

game.excelManager.SetWins(winCounter);

foreach (var kvp in winCounter)
    Console.WriteLine($"{kvp.Key} - {kvp.Value}");

Console.Beep();
Console.Beep();
Console.Beep();
Console.Beep();

