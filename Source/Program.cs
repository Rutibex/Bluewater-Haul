using BluewaterHaul;
if(args.Contains("--verify")){Verification.Run();return;}
try {using var game=new SarniaGame(args.Contains("--smoke"));game.Run();}
catch(Exception ex){var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BluewaterHaul");Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"crash.txt"),ex.ToString());Console.Error.WriteLine(ex);throw;}
