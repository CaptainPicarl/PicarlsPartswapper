using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Security.AccessControl;
using System.Security;
using System.Numerics;
using System.Drawing;
using System.Runtime.Intrinsics.Arm;
using ScottPlot;
using Microsoft.VisualBasic;
using System.Net;
using System.ComponentModel.Design.Serialization;
using static PartSwapperXMLSE.WCWeaponDefinition;
using System.IO;

namespace PartSwapperXMLSE
{
    public class WeaponcoreStats
    {
        public static List<I_WCDefinition> wcDefinitions = new List<I_WCDefinition>();
        public static NameResolver nameResolver = new NameResolver();

        public bool vanillaDefinitions = false;

        private readonly string _modsDirOptKey = "modsDir";
        private ConfigOptions _config;

        public static ScottPlot.Color color1 = ScottPlot.Color.FromHex("#FF7700");
        public static ScottPlot.Color color3 = ScottPlot.Color.FromHex("#333333");
        public static ScottPlot.Color color2 = ScottPlot.Color.FromHex("#949494");

        public WeaponcoreStats(ConfigOptions config)
        {
            this._config = config;

            LoadWCDefsViaModFolders(_config.GetOption(_modsDirOptKey));
        }

        // declaration string to vector3
        public static Vector3 declStringToVector3(string declarationString)
        {
            Vector3 result = new Vector3();

            if (declarationString == null || declarationString.Length == 0)
            {
                return result;
            }
            else
            {
                try
                {
                    int xStrIndex = declarationString.IndexOf('x') + "x".Length + 1;
                    int xStrEndIndex = declarationString.IndexOf(',', xStrIndex);

                    int yStrIndex = declarationString.IndexOf('y') + "y".Length + 1;
                    int yStrEndIndex = declarationString.IndexOf(',', yStrIndex);

                    int zStrIndex = declarationString.IndexOf('z') + "z".Length + 1;
                    int zStrEndIndex = declarationString.IndexOf(')', zStrIndex);

                    string xValueStr = declarationString.Substring(xStrIndex, xStrEndIndex - xStrIndex);
                    string yValueStr = declarationString.Substring(yStrIndex, yStrEndIndex - yStrIndex);
                    string zValueStr = declarationString.Substring(zStrIndex, zStrEndIndex - zStrIndex);

                    float xValParsed = float.Parse(xValueStr);
                    float yValParsed = float.Parse(yValueStr);
                    float zValParsed = float.Parse(zValueStr);

                    result = new Vector3(xValParsed, yValParsed, zValParsed);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Failure in declStringToVector3!\n");
                    Trace.WriteLine(ex + "\n");
                }
                return result;
            }
        }

        // declaration string to color
        public static System.Drawing.Color declStringToColor(string declarationString)
        {
            System.Drawing.Color result = new System.Drawing.Color();

            if (declarationString == null || declarationString.Length == 0)
            {
                return result;
            }
            else
            {
                try
                {
                    int redStrIndex = declarationString.IndexOf("red") + "red".Length + 1;
                    int redStrEndIndex = declarationString.IndexOf(',', redStrIndex);

                    int greenStrIndex = declarationString.IndexOf("green") + "green".Length + 1;
                    int greenStrEndIndex = declarationString.IndexOf(',', greenStrIndex);

                    int blueStrIndex = declarationString.IndexOf("blue") + "blue".Length + 1;
                    int blueStrEndIndex = declarationString.IndexOf(',', blueStrIndex);

                    int alphaStrIndex = declarationString.IndexOf("alpha") + "alpha".Length + 1;
                    int alphaStrEndIndex = declarationString.IndexOf(')', alphaStrIndex);

                    string redValueStr = declarationString.Substring(redStrIndex, redStrEndIndex - redStrIndex);
                    string greenValueStr = declarationString.Substring(greenStrIndex, greenStrEndIndex - greenStrIndex);
                    string blueValueStr = declarationString.Substring(blueStrIndex, blueStrEndIndex - blueStrIndex);
                    string alphaValueStr = declarationString.Substring(alphaStrIndex, alphaStrEndIndex - alphaStrIndex);

                    float redValParsed = float.Parse(redValueStr);
                    float greenValParsed = float.Parse(greenValueStr);
                    float blueValParsed = float.Parse(blueValueStr);
                    float alphaValParsed = float.Parse(alphaValueStr);

                    result = System.Drawing.Color.FromArgb(((int)alphaValParsed), (int)redValParsed, (int)greenValParsed, (int)blueValParsed);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Failure in declStringToColor!\n");
                    Trace.WriteLine(ex + "\n");
                }
                return result;
            }
        }

        public static string stripFloatChar(string floatString)
        {
            if (floatString == null)
            {
                return "";
            }
            else
            {
                if (floatString.Contains('f'))
                {
                    return floatString.Remove(floatString.Length - 1);
                }
                else
                {
                    return floatString;
                }
            }

        }

        private void LoadNonWCViaModFolder(string modFolderPath)
        {
            // modpath should ideally be the root folder that contains all the mod folders we want to iterate through

            DirectoryInfo[] modFolderDirectories;

            DirectoryInfo modFolderRoot;
            DirectoryInfo dataDirectory;

            FileInfo[] dataFiles;

            FileInfo ammoSBCFileInfo;
            FileInfo weaponsSBCFileInfo;

            XElement ammoSBCXRoot;
            XElement weaponSBCXRoot;

            XElement ammosNode;
            XElement weaponsNode;

            try
            {
                if (modFolderPath == null || modFolderPath.Equals(""))
                {
                    // This happens when the user cancels out of the mod selection folder.
                    // We'll handle this more gracefully in the future. Maybe.
                    return;
                }

                // assign the modFolderRoot
                modFolderRoot = new DirectoryInfo(modFolderPath);

            }
            catch (Exception ex)
            {
                throw new InvalidDataException("modpath invalid!");
            }



            if (!modFolderRoot.FullName.Contains("workshop\\content\\244850"))
            {
                throw new InvalidDataException("Error: Invalid folder!\nPath should end in: workshop\\content\\244850\n");
            }

            // We iterate through each of those mod directories, to begin our search for relevant files that contain part definitions
            foreach (DirectoryInfo modFolder in modFolderRoot.GetDirectories())
            {
                modFolderDirectories = modFolder.GetDirectories();

                // First, check if the modFolder (specifically: its directories via modFolderSubDirectories) contains a "Data" folder
                if (modFolderDirectories.Any(x => x.Name == "Data"))
                {
                    // If a "Data" folder exists, look for the BlockVariantGroupsSBC_BlockVariantGroup files
                    // TODO: Maybe someday we can also use BlockCategories? Not sure which would be best...
                    dataDirectory = new DirectoryInfo(System.IO.Path.Combine(modFolder.FullName, "Data"));
                    dataFiles = dataDirectory.GetFiles();

                    // If one of the dataFiles is named "BlockVariantGroups.sbc"
                    if (dataFiles.Any(x => x.Name.Equals("Ammos.sbc")))
                    {
                        // Get the fileInfo via path-combining the maxDistanceData directory path with the word "BlockVariantGroups". Then loaad the file into XML.
                        ammoSBCFileInfo = new FileInfo(System.IO.Path.Combine(dataDirectory.FullName, "Ammos.sbc"));

                        // Loading the ammoSBCXRoot file into an XMLDocument we can read
                        ammoSBCXRoot = XElement.Load(ammoSBCFileInfo.OpenRead());

                        ammosNode = ammoSBCXRoot.Element("Ammos");

                        foreach (XElement ammo in ammoSBCXRoot.Elements())
                        {
                            VanillaAmmoStats newAS = new VanillaAmmoStats(ammo);
                            //this._ammoStatList.Add(newAS);
                        }
                    }

                    if (dataFiles.Any(x => x.Name.Equals("Weapons.sbc")))
                    {
                        // Get the fileInfo via path-combining the maxDistanceData directory path with the word "BlockVariantGroups". Then load the file into XML.
                        ammoSBCFileInfo = new FileInfo(System.IO.Path.Combine(dataDirectory.FullName, "Weapons.sbc"));

                        // Loading the ammoSBCXRoot file into an XMLDocument we can read
                        weaponSBCXRoot = XElement.Load(ammoSBCFileInfo.OpenRead());

                        weaponsNode = weaponSBCXRoot.Element("Weapons");

                        foreach (XElement ammo in weaponSBCXRoot.Elements())
                        {
                            VanillaWeaponStats newAS = new VanillaWeaponStats(ammo);
                            //this._weaponStatList.Add(newAS);
                        }
                    }

                }

            }
        }

        // Loads weaponcore values
        private void LoadWCDefsViaModFolders(string modFolderPath)
        {
            // modpath should ideally be the root folder that contains all the mod folders we want to iterate through
            string fileText;

            SyntaxTree syntaxTree;

            CompilationUnitSyntax syntaxTreeRoot;

            DirectoryInfo[] modFolderDirectories;

            DirectoryInfo modFolderRoot;
            DirectoryInfo dataDirectory;
            DirectoryInfo corepartsDirectory;

            FileInfo[] dataFiles;

            FileInfo ammoSBCFileInfo;
            FileInfo weaponsSBCFileInfo;

            XElement ammoSBCXRoot;
            XElement weaponSBCXRoot;

            XElement ammosNode;
            XElement weaponsNode;

            try
            {
                if (modFolderPath == null || modFolderPath.Equals(""))
                {
                    // This happens when the user cancels out of the mod selection folder.
                    // We'll handle this more gracefully in the future. Maybe.
                    return;
                }

                // assign the modFolderRoot
                modFolderRoot = new DirectoryInfo(modFolderPath);

            }
            catch (Exception ex)
            {
                throw new InvalidDataException("modpath invalid!");
            }



            if (!modFolderRoot.FullName.Contains("workshop\\content\\244850"))
            {
                throw new InvalidDataException("Error: Invalid folder!\nPath should end in: workshop\\content\\244850\n");
            }

            // We iterate through each of those mod directories, to begin our search for relevant files that contain part definitions
            foreach (DirectoryInfo modFolder in modFolderRoot.GetDirectories())
            {
                modFolderDirectories = modFolder.GetDirectories();

                // First, check if the modFolder (specifically: its directories via modFolderSubDirectories) contains a "Data" folder
                if (modFolderDirectories.Any(x => x.Name == "Data"))
                {
                    dataDirectory = new DirectoryInfo(Path.Combine(modFolder.FullName, "Data"));

                    if (Path.Exists(Path.Combine(modFolder.FullName, "Data", "Scripts", "CoreParts")))
                    {
                        corepartsDirectory = new DirectoryInfo(System.IO.Path.Combine(modFolder.FullName, "Data", "Scripts", "CoreParts"));
                        dataFiles = corepartsDirectory.GetFiles();

                        // EVERYTHING BELOW THIS NEEDS REWRITTEN FOR USE IN WC
                        // CURRENT ISSUE: How do I import and read a .cs file?
                        // If one of the dataFiles is named "BlockVariantGroups.sbc"

                        foreach (FileInfo file in dataFiles)
                        {

                            fileText = File.ReadAllText(file.FullName);

                            syntaxTree = CSharpSyntaxTree.ParseText(fileText);
                            syntaxTreeRoot = syntaxTree.GetCompilationUnitRoot();

                            //ArmorDefSyntaxWalker armorWalker = new ArmorDefSyntaxWalker();
                            WCWeaponWalker weaponWalker = new WCWeaponWalker();
                            WCAmmoWalker ammoWalker = new WCAmmoWalker();

                            weaponWalker.Visit(syntaxTreeRoot);
                            ammoWalker.Visit(syntaxTreeRoot);
                            //armorWalker.Visit(syntaxTreeRoot);
                        }
                    }
                }
            }
        }

        public static void wcDefAssignHardpointDefsLowLevel(SeparatedSyntaxList<ExpressionSyntax> hardpointExpressions, ref hardpointDef hardpointDef)
        {

            foreach (ExpressionSyntax expression in hardpointExpressions)
            {
                switch (expression)
                {
                    case AssignmentExpressionSyntax madAES:

                        if (madAES.Left.ToString().Equals("PartName"))
                        {
                            switch (madAES.Right)
                            {
                                case ImplicitArrayCreationExpressionSyntax madIACES:
                                    // foreach threat in Threats
                                    foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                    {

                                        switch (madES)
                                        {
                                            case ObjectCreationExpressionSyntax madOCES:
                                                break;

                                            case IdentifierNameSyntax madINS:
                                                Trace.WriteLine("Adding threat " + madINS.ToString() + "\n");
                                                try
                                                {
                                                    //ammoDefinition.targetingDefinition.threats.Add(madINS.ToString());
                                                }
                                                catch
                                                {
                                                    Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.threats\n");
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {madES} in: HardPoint.PartName.dnExpression2");
                                        }

                                    }
                                    break;

                                case LiteralExpressionSyntax madLES:
                                    Trace.WriteLine("Found partname: " + madLES.ToString() + "\n");
                                    try
                                    {
                                        hardpointDef.partName = madLES.ToString();
                                    }
                                    catch
                                    {
                                        Trace.WriteLine($"Failure to change hardpointDef.partName\n");
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.PartName.dnAES2.Right");
                            }

                        }

                        if (madAES.Left.ToString().Equals("DeviateShotAngle"))
                        {
                            switch (madAES.Right)
                            {
                                case ImplicitArrayCreationExpressionSyntax madIACES:
                                    foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                    {
                                        switch (madES)
                                        {
                                            case LiteralExpressionSyntax madLES:
                                                try
                                                {
                                                    hardpointDef.deviateShotAngle = float.Parse(stripFloatChar(madLES.ToString()));
                                                }
                                                catch
                                                {
                                                    Trace.WriteLine($"Failure to change hardpointDef.deviateShotAngle\n");
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {madES.ToString()} in: HardPoint.DeviantShotAngle.dnExpression2.ToString()");
                                        }
                                    }
                                    break;

                                case LiteralExpressionSyntax madIACES:

                                    try
                                    {
                                        hardpointDef.deviateShotAngle = float.Parse(stripFloatChar(madIACES.ToString()));
                                    }
                                    catch
                                    {
                                        Trace.WriteLine($"Failure to change hardpointDef.deviateShotAngle\n");
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.DeviantShotAngle.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("AimingTolerance"))
                        {
                            Trace.WriteLine("Found AimingTolerance\n");

                            switch (madAES.Right)
                            {
                                case LiteralExpressionSyntax madLES:
                                    try
                                    {
                                        hardpointDef.aimingTolerance = float.Parse(stripFloatChar(madLES.ToString()));
                                    }
                                    catch
                                    {
                                        Trace.WriteLine($"Failure to change hardpointDef.aimingTolerance\n");
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.AimingTolerance.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("AimLeadingPrediction"))
                        {
                            Trace.WriteLine("Found AimLeadingPrediction\n");
                            switch (madAES.Right)
                            {
                                case LiteralExpressionSyntax madLES:
                                    try
                                    {
                                        hardpointDef.aimLeadingPrediction = madLES.ToString();
                                    }
                                    catch
                                    {
                                        Trace.WriteLine($"Failure to change hardpointDef.aimLeadingPrediction\n");
                                    }
                                    break;
                                case IdentifierNameSyntax madINS:
                                    try
                                    {
                                        hardpointDef.aimLeadingPrediction = madINS.ToString();
                                    }
                                    catch
                                    {
                                        Trace.WriteLine($"Failure to change hardpointDef.aimLeadingPrediction\n");
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.AimLeadingPrediction.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("DelayCeaseFire"))
                        {
                            Trace.WriteLine("Found DelayCeaseFire\n");
                            switch (madAES.Right)
                            {
                                case LiteralExpressionSyntax madLES:
                                    try
                                    {
                                        hardpointDef.delayCeaseFire = float.Parse(stripFloatChar(madLES.ToString()));

                                    }
                                    catch
                                    {
                                        Trace.WriteLine($"Failure to change hardpointDef.delayCeaseFire\n");
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.DelayCeaseFire.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("AddToleranceToTracking"))
                        {
                            Trace.WriteLine("Found AddToleranceToTracking\n");

                            switch (madAES.Right)
                            {
                                case LiteralExpressionSyntax madLES:
                                    hardpointDef.addToleranceToTracking = Boolean.Parse(madLES.ToString());
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.AddToleranceToTracking.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("CanShootSubmerged"))
                        {
                            Trace.WriteLine("Found CanShootSubmerged\n");

                            switch (madAES.Right)
                            {
                                case LiteralExpressionSyntax madLES:
                                    hardpointDef.canShootSubmerged = Boolean.Parse(madLES.ToString());
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardPoint.CanShootSubmerged.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("Ui"))
                        {
                            Trace.WriteLine("Found Ui\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found UI Expression: " + expression2 + "\n");

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:
                                                if (madAES2.Left.ToString().Equals("RateOfFire"))
                                                {
                                                    hardpointDef.uiDefinition.rateOfFire = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("DamageModifier"))
                                                {
                                                    hardpointDef.uiDefinition.damageModifier = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("ToggleGuidance"))
                                                {
                                                    hardpointDef.uiDefinition.toggleGuidance = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("EnableOverload"))
                                                {
                                                    hardpointDef.uiDefinition.enableOverload = Boolean.Parse(madAES2.Right.ToString());
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: Ui.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax ids:
                                    hardpointDef.uiDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.uiDef>(ids.Identifier);
                                    break;
                                //throw new Exception("Unimplemented!");
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: Ui.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("Ai"))
                        {
                            Trace.WriteLine("Found Ai\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found AI Expression: " + expression2 + "\n");

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:
                                                if (madAES2.Left.ToString().Equals("TrackTargets"))
                                                {
                                                    hardpointDef.aiDefinition.trackTargets = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("TurretAttached"))
                                                {
                                                    hardpointDef.aiDefinition.turretAttached = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("TurretController"))
                                                {
                                                    hardpointDef.aiDefinition.turretAttached = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("PrimaryTracking"))
                                                {
                                                    hardpointDef.aiDefinition.primaryTracking = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("LockOnFocus"))
                                                {
                                                    hardpointDef.aiDefinition.lockOnFocus = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("SuppressFire"))
                                                {
                                                    hardpointDef.aiDefinition.suppressFire = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("OverrideLeads"))
                                                {
                                                    hardpointDef.aiDefinition.overrideLeads = Boolean.Parse(madAES2.Right.ToString());
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: AI.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax ids:
                                    hardpointDef.aiDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.aiDef>(ids.Identifier);
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: AI.dnAES2.Right");
                            }
                        }

                        if (madAES.Left.ToString().Equals("HardWare"))
                        {
                            Trace.WriteLine("Found HardWare\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found HardWare Expression: " + expression2 + "\n");

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:
                                                if (madAES2.Left.ToString().Equals("RotateRate"))
                                                {
                                                    hardpointDef.hardwareDefinition.rotateRate = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("ElevateRate"))
                                                {
                                                    hardpointDef.hardwareDefinition.elevateRate = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("MinAzimuth"))
                                                {
                                                    hardpointDef.hardwareDefinition.minAzimuth = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("MaxAzimuth"))
                                                {
                                                    hardpointDef.hardwareDefinition.maxAzimuth = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }


                                                if (madAES2.Left.ToString().Equals("MinElevation"))
                                                {
                                                    hardpointDef.hardwareDefinition.minElevation = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("MaxElevation"))
                                                {
                                                    hardpointDef.hardwareDefinition.maxElevation = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("HomeAzimuth"))
                                                {
                                                    hardpointDef.hardwareDefinition.homeAzimuth = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("HomeElevation"))
                                                {
                                                    hardpointDef.hardwareDefinition.homeElevation = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("InventorySize"))
                                                {
                                                    hardpointDef.hardwareDefinition.inventorySize = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("IdlePower"))
                                                {
                                                    hardpointDef.hardwareDefinition.idlePower = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("FixedOffset"))
                                                {
                                                    hardpointDef.hardwareDefinition.fixedOffset = Boolean.Parse(madAES2.Right.ToString());
                                                }


                                                if (madAES2.Left.ToString().Equals("Offset"))
                                                {
                                                    hardpointDef.hardwareDefinition.offset = declStringToVector3(madAES2.Right.ToString());

                                                }


                                                if (madAES2.Left.ToString().Equals("Type"))
                                                {
                                                    hardpointDef.hardwareDefinition.type = madAES2.Right.ToString();
                                                }

                                                if (madAES2.Left.ToString().Equals("CriticalReaction"))
                                                {
                                                    Trace.WriteLine("Found CriticalReaction definition.\n");

                                                    switch (madAES2.Right)
                                                    {
                                                        case ObjectCreationExpressionSyntax madOCES:
                                                            foreach (ExpressionSyntax expression4 in madOCES.Initializer.Expressions)
                                                            {
                                                                switch (expression4)
                                                                {
                                                                    case AssignmentExpressionSyntax madAES3:
                                                                        if (madAES3.Left.ToString().Equals("Enable"))
                                                                        {
                                                                            hardpointDef.hardwareDefinition.criticalReaction.enable = bool.Parse(madAES3.Right.ToString());
                                                                        }

                                                                        if (madAES3.Left.ToString().Equals("DefaultArmedTimer"))
                                                                        {
                                                                            hardpointDef.hardwareDefinition.criticalReaction.defaultArmedTimer = float.Parse(stripFloatChar(madAES3.Right.ToString()));
                                                                        }

                                                                        if (madAES3.Left.ToString().Equals("PreArmed"))
                                                                        {
                                                                            hardpointDef.hardwareDefinition.criticalReaction.preArmed = bool.Parse(madAES3.Right.ToString());
                                                                        }

                                                                        if (madAES3.Left.ToString().Equals("TerminalControls"))
                                                                        {
                                                                            hardpointDef.hardwareDefinition.criticalReaction.terminalControls = bool.Parse(madAES3.Right.ToString());
                                                                        }
                                                                        break;
                                                                    default:
                                                                        throw new Exception($"Unknown syntax {expression4} in: HardWare.CriticalReaction.expression4");
                                                                }
                                                            }
                                                            break;
                                                        case IdentifierNameSyntax rightINS:
                                                            hardpointDef.hardwareDefinition.criticalReaction = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.hardwareDef.critialDef>(rightINS.Identifier);
                                                            break;
                                                        default:
                                                            throw new Exception($"Unknown syntax {madAES2.Right} in: HardWare.CriticalReaction.assignmentAES.Right");
                                                    }
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: HardWare.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax rightINS:
                                    hardpointDef.hardwareDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.hardwareDef>(rightINS.Identifier);
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: HardWare.MadAES2.Right");
                            }
                            break;
                        }

                        if (madAES.Left.ToString().Equals("Other"))
                        {
                            Trace.WriteLine("Found Other\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found Other Expression: " + expression2 + "\n");

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:
                                                if (madAES2.Left.ToString().Equals("ConstructPartCap"))
                                                {
                                                    hardpointDef.otherDefinition.constructPartCap = float.Parse(stripFloatChar((madAES2.Right.ToString())));
                                                }

                                                if (madAES2.Left.ToString().Equals("RotateBarrelAxis"))
                                                {
                                                    hardpointDef.otherDefinition.rotateBarrelAxis = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("EnergyPriority"))
                                                {
                                                    hardpointDef.otherDefinition.energyPriority = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("MuzzleCheck"))
                                                {
                                                    hardpointDef.otherDefinition.muzzleCheck = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("Debug"))
                                                {
                                                    hardpointDef.otherDefinition.debug = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("RestrictionRadius"))
                                                {
                                                    hardpointDef.otherDefinition.restrictionRadius = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }


                                                if (madAES2.Left.ToString().Equals("CheckInflatedBox"))
                                                {
                                                    hardpointDef.otherDefinition.checkInflatedBox = Boolean.Parse(madAES2.Right.ToString());

                                                }


                                                if (madAES2.Left.ToString().Equals("CheckForAnyWeapon"))
                                                {
                                                    hardpointDef.otherDefinition.checkForAnyWeapon = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: Other.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax idns:
                                    hardpointDef.otherDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.otherDef>(idns.Identifier);
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: Other.dnAES2.Right");
                            }
                            break;
                        }

                        if (madAES.Left.ToString().Equals("Loading"))
                        {
                            Trace.WriteLine("Found Loading\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found Loading Expression: " + expression2 + "\n");

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:
                                                if (madAES2.Left.ToString().Equals("RateOfFire"))
                                                {
                                                    hardpointDef.loadingDefinition.rateOfFire = float.Parse(stripFloatChar((madAES2.Right.ToString())));
                                                }

                                                if (madAES2.Left.ToString().Equals("BarrelSpinRate"))
                                                {
                                                    hardpointDef.loadingDefinition.barrelSpinRate = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("BarrelsPerShot"))
                                                {
                                                    hardpointDef.loadingDefinition.barrelsPerShot = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("TrajectilesPerBarrel"))
                                                {
                                                    hardpointDef.loadingDefinition.trajectilesPerBarrel = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("DelayUntilFire"))
                                                {
                                                    hardpointDef.loadingDefinition.delayUntilFire = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("HeatPerShot"))
                                                {
                                                    hardpointDef.loadingDefinition.heatPerShot = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }


                                                if (madAES2.Left.ToString().Equals("MaxHeat"))
                                                {
                                                    hardpointDef.loadingDefinition.maxHeat = float.Parse(stripFloatChar(madAES2.Right.ToString()));

                                                }

                                                if (madAES2.Left.ToString().Equals("Cooldown"))
                                                {
                                                    hardpointDef.loadingDefinition.cooldown = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("HeatSinkRate"))
                                                {
                                                    hardpointDef.loadingDefinition.heatsinkRate = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("DegradeRof"))
                                                {
                                                    hardpointDef.loadingDefinition.degradeRof = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("ShotsInBurst"))
                                                {
                                                    hardpointDef.loadingDefinition.shotsInBurst = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("DelayAfterBurst"))
                                                {
                                                    hardpointDef.loadingDefinition.delayAfterBurst = float.Parse(stripFloatChar(madAES2.Right.ToString()));
                                                }

                                                if (madAES2.Left.ToString().Equals("FireFull"))
                                                {
                                                    hardpointDef.loadingDefinition.fireFull = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("GiveUpAfter"))
                                                {
                                                    hardpointDef.loadingDefinition.giveUpAfter = Boolean.Parse(madAES2.Right.ToString());
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: Loading.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax rightINS:
                                    hardpointDef.loadingDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.loadingDef>(rightINS.Identifier);
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: Loading.dnAES2.Right");
                            }
                            break;
                        }

                        if (madAES.Left.ToString().Equals("Audio"))
                        {
                            Trace.WriteLine("Found Audio\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found Audio Expression: " + expression2 + "\n");

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:
                                                if (madAES2.Left.ToString().Equals("PreFiringSound"))
                                                {
                                                    hardpointDef.audioDefinition.preFiringSound = madAES2.Right.ToString();
                                                }

                                                if (madAES2.Left.ToString().Equals("FiringSoundPerShot"))
                                                {
                                                    hardpointDef.audioDefinition.firingSoundPerShot = Boolean.Parse(madAES2.Right.ToString());
                                                }

                                                if (madAES2.Left.ToString().Equals("ReloadSound"))
                                                {
                                                    hardpointDef.audioDefinition.reloadSound = madAES2.Right.ToString();
                                                }

                                                if (madAES2.Left.ToString().Equals("NoAmmoSound"))
                                                {
                                                    hardpointDef.audioDefinition.noAmmoSound = madAES2.Right.ToString();
                                                }

                                                if (madAES2.Left.ToString().Equals("HardPointRotationSound"))
                                                {
                                                    hardpointDef.audioDefinition.hardpointRotationSound = madAES2.Right.ToString();
                                                }

                                                if (madAES2.Left.ToString().Equals("BarrelRotationSound"))
                                                {
                                                    hardpointDef.audioDefinition.barrelrotationSound = madAES2.Right.ToString();
                                                }


                                                if (madAES2.Left.ToString().Equals("FireSoundEndDelay"))
                                                {
                                                    hardpointDef.audioDefinition.fireSoundEndDelay = float.Parse(stripFloatChar((madAES2.Right.ToString())));

                                                }

                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: Audio.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax rightINS:
                                    hardpointDef.audioDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.audioDef>(rightINS.Identifier);
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: Audio.dnAES2.Right");
                            }
                            break;
                        }

                        if (madAES.Left.ToString().Equals("Graphics"))
                        {
                            Trace.WriteLine("Found Graphics\n");

                            switch (madAES.Right)
                            {
                                case ObjectCreationExpressionSyntax madOCE:

                                    foreach (ExpressionSyntax expression2 in madOCE.Initializer.Expressions)
                                    {
                                        Trace.WriteLine("Found Graphics Expression: " + expression2 + "\n");

                                        particleDef particleDefIterator = new particleDef();
                                        particleDef.particleOptionDef particleDefIteratorOpts = new particleDef.particleOptionDef();

                                        switch (expression2)
                                        {
                                            case AssignmentExpressionSyntax madAES2:

                                                Trace.WriteLine($"Found Effect definition: {madAES2.ToString()}\n");

                                                switch (madAES2.Right)
                                                {
                                                    case ObjectCreationExpressionSyntax madOCES:

                                                        foreach (ExpressionSyntax expression4 in madOCES.Initializer.Expressions)
                                                        {
                                                            switch (expression4)
                                                            {
                                                                case AssignmentExpressionSyntax madAES3:
                                                                    if (madAES3.Left.ToString().Equals("Name"))
                                                                    {
                                                                        particleDefIterator.name = madAES3.Right.ToString();
                                                                    }

                                                                    if (madAES3.Left.ToString().Equals("Color"))
                                                                    {
                                                                        particleDefIterator.color = declStringToColor(madAES3.Right.ToString());
                                                                    }

                                                                    if (madAES3.Left.ToString().Equals("Offset"))
                                                                    {
                                                                        particleDefIterator.offset = declStringToVector3(madAES3.Right.ToString());
                                                                    }

                                                                    if (madAES3.Left.ToString().Equals("Extras"))
                                                                    {
                                                                        switch (madAES3.Right)
                                                                        {
                                                                            case ObjectCreationExpressionSyntax madOCES3:
                                                                                foreach (ExpressionSyntax madES in madOCES3.Initializer.Expressions)
                                                                                {
                                                                                    switch (madES)
                                                                                    {
                                                                                        case AssignmentExpressionSyntax madAES4:
                                                                                            Trace.WriteLine("Found extras AssignmentExpressionSyntax: " + madAES4 + "\n");

                                                                                            if (madAES4.Left.ToString().Equals("Loop"))
                                                                                            {
                                                                                                particleDefIteratorOpts.loop = Boolean.Parse(madAES4.Right.ToString());
                                                                                            }

                                                                                            if (madAES4.Left.ToString().Equals("Restart"))
                                                                                            {
                                                                                                particleDefIteratorOpts.restart = Boolean.Parse(madAES4.Right.ToString());
                                                                                            }

                                                                                            if (madAES4.Left.ToString().Equals("MaxDistance"))
                                                                                            {
                                                                                                particleDefIteratorOpts.maxDistance = float.Parse(stripFloatChar(madAES4.Right.ToString()));
                                                                                            }

                                                                                            if (madAES4.Left.ToString().Equals("MaxDuration"))
                                                                                            {
                                                                                                particleDefIteratorOpts.maxDuration = float.Parse(stripFloatChar(madAES4.Right.ToString()));
                                                                                            }

                                                                                            if (madAES4.Left.ToString().Equals("Scale"))
                                                                                            {
                                                                                                particleDefIteratorOpts.scale = float.Parse(stripFloatChar(madAES4.Right.ToString()));
                                                                                            }
                                                                                            break;
                                                                                        default:
                                                                                            throw new Exception($"Unknown syntax {madES} in: Graphics.Effect.dnExpression2");
                                                                                    }
                                                                                }
                                                                                particleDefIterator.extras = particleDefIteratorOpts;
                                                                                break;
                                                                            default:
                                                                                throw new Exception($"Unknown syntax {madAES3.Right} in: Graphics.Effect.madAES3.Right");
                                                                        }
                                                                    }
                                                                    break;
                                                                default:
                                                                    throw new Exception($"Unknown syntax {expression4} in: Graphics.expression4");
                                                            }
                                                        }
                                                        hardpointDef.graphicsDefinition.particleDefinitions.Add(particleDefIterator);

                                                        break;
                                                    default:
                                                        throw new Exception($"Unknown syntax {madAES2.Right} in: Graphics.Effect.assignmentAES.Right");
                                                }


                                                break;
                                            default:
                                                throw new Exception($"Unknown syntax {expression2} in: Graphics.Effect.expression2");
                                        }
                                    }
                                    break;
                                case IdentifierNameSyntax rightINS:
                                    hardpointDef.graphicsDefinition = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.hardpointDef.hardpointParticleDef>(rightINS.Identifier);
                                    break;
                                default:
                                    throw new Exception($"Unknown syntax {madAES.Right} in: Graphics.dnAES2.Right");
                            }
                            break;
                        }

                        break;
                    default:
                        throw new Exception($"Unknown syntax {expression} in: HardPoint.PartName.dnExpression");
                }

            }

        }

        public static void weaponDefParseHardpoint(ExpressionSyntax dataNode, ref WCWeaponDefinition.hardpointDef hardpointDef)
        {

            switch (dataNode)
            {
                case AssignmentExpressionSyntax dnAES:
                    switch (dnAES.Right)
                    {
                        case ObjectCreationExpressionSyntax rightOCES:
                            wcDefAssignHardpointDefsLowLevel(rightOCES.Initializer.Expressions, ref hardpointDef);
                            break;
                        default:
                            throw new Exception($"Unknown syntax {dnAES.Right} in: HardPoint.assignment.Right");
                    }
                    break;
                default:
                    throw new Exception($"Unknown syntax {dataNode} in: HardPoint");
            }
        }

        public static void weaponDefParseTargeting(ExpressionSyntax dataNode, ref WCWeaponDefinition.targetingDef targetingDef)
        {
            switch (dataNode)
            {
                case AssignmentExpressionSyntax dnAES:
                    // Check the right-hand side of assignments dnExpression 
                    switch (dnAES.Right)
                    {
                        case ObjectCreationExpressionSyntax rightOCES:
                            foreach (SyntaxNode syntaxNode in dnAES.Right.ChildNodes())
                            {
                                switch (syntaxNode)
                                {
                                    case IdentifierNameSyntax syntaxNodeINS:

                                        if (syntaxNodeINS.Identifier.Text.Equals("TargetingDef"))
                                        {
                                            switch (rightOCES.Initializer)
                                            {
                                                case InitializerExpressionSyntax:

                                                    foreach (ExpressionSyntax expression in rightOCES.Initializer.Expressions)
                                                    {
                                                        switch (expression)
                                                        {
                                                            case AssignmentExpressionSyntax madAES:

                                                                if (madAES.Left.ToString().Equals("Threats"))
                                                                {
                                                                    switch (madAES.Right)
                                                                    {
                                                                        case ImplicitArrayCreationExpressionSyntax madIACES:
                                                                            // foreach threat in Threats
                                                                            foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                                            {

                                                                                switch (madES)
                                                                                {
                                                                                    case ObjectCreationExpressionSyntax madOCES:
                                                                                        break;
                                                                                    case IdentifierNameSyntax madINS:
                                                                                        Trace.WriteLine("Adding threat " + madINS.ToString() + "\n");
                                                                                        try
                                                                                        {
                                                                                            targetingDef.threats.Add(madINS.ToString());
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.threats");
                                                                                        }
                                                                                        break;
                                                                                    default:
                                                                                        throw new Exception($"Found unknown syntax in Targeting.TargetingDef.Threats");
                                                                                }

                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.TargetingDef.Threats");
                                                                    }

                                                                }

                                                                if (madAES.Left.ToString().Equals("SubSystems"))
                                                                {
                                                                    switch (madAES.Right)
                                                                    {
                                                                        case ImplicitArrayCreationExpressionSyntax madIACES:
                                                                            foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                                            {
                                                                                switch (madES)
                                                                                {
                                                                                    case IdentifierNameSyntax madINS:
                                                                                        Trace.WriteLine("Adding SubSystem target: " + madINS.ToString() + "\n");

                                                                                        try
                                                                                        {
                                                                                            targetingDef.subsystems.Add(madINS.ToString());
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.subsystems");
                                                                                        }
                                                                                        break;

                                                                                    default:
                                                                                        throw new Exception($"Found unknown syntax in Targeting.SubSystems");
                                                                                }
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.SubSystems");
                                                                    }

                                                                }

                                                                if (madAES.Left.ToString().Equals("ClosestFirst"))
                                                                {
                                                                    Trace.WriteLine("Found ClosestFirst\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.closestFirst = Boolean.Parse(madLES.ToString());
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.closestFirst\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.ClosestFirst");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("IgnoreDumbProjectiles"))
                                                                {
                                                                    Trace.WriteLine("Found IgnoreDumbProjectiles\n");
                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.ignoreDumbProjectiles = Boolean.Parse(madLES.ToString());
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.ignoreDumbProjectiles\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.IgnoreDumbProjectiles");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("LockedSmartOnly"))
                                                                {
                                                                    Trace.WriteLine("Found LockedSmartOnly\n");
                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.lockedSmartOnly = Boolean.Parse(madLES.ToString());
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.ignoreDumbProjectiles\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.LockedSmartOnly");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("MinimumDiameter"))
                                                                {
                                                                    Trace.WriteLine("Found MinimumDiameter\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.minimumDiameter = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.minimumDiameter\n");
                                                                            }


                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.MinimumDiameter");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("MaximumDiameter"))
                                                                {
                                                                    Trace.WriteLine("Found MaximumDiameter\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.maximumDiameter = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.maximumDiameter\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.MaximumDiameter");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("MaxTargetDistance"))
                                                                {
                                                                    Trace.WriteLine("Found MaxTargetDistance\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.maxTargetDistance = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.maxTargetDistance\n");
                                                                            }
                                                                            break;
                                                                        case IdentifierNameSyntax rightINS:
                                                                            targetingDef.maxTargetDistance = nameResolver.resolveSyntaxIdentifier<float>(rightINS.Identifier);
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.MaxTargetDistance");
                                                                    }
                                                                }


                                                                if (madAES.Left.ToString().Equals("MinTargetDistance"))
                                                                {
                                                                    Trace.WriteLine("Found MinTargetDistance\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.minTargetDistance = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.minTargetDistance\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.MinTargetDistance");
                                                                    }
                                                                }


                                                                if (madAES.Left.ToString().Equals("TopTargets"))
                                                                {
                                                                    Trace.WriteLine("Found TopTargets\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.topTargets = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.topTargets\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.TopTargets");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("TopBlocks"))
                                                                {
                                                                    Trace.WriteLine("Found TopBlocks\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.topBlocks = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.topBlocks\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.TopBlocks");
                                                                    }
                                                                }

                                                                if (madAES.Left.ToString().Equals("StopTrackingSpeed"))
                                                                {
                                                                    Trace.WriteLine("Found StopTrackingSpeed\n");

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madLES:
                                                                            try
                                                                            {
                                                                                targetingDef.stopTrackingSpeed = float.Parse(stripFloatChar(madLES.ToString()));
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.targetingDefinition.stopTrackingSpeed\n");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Found unknown syntax in Targeting.stopTrackingSpeed");
                                                                    }
                                                                }
                                                                break;
                                                            default:
                                                                break;
                                                        }

                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                            break;

                        case IdentifierNameSyntax rightINS:
                            // TODO: We need to handle when a user makes a reference to an existing definition that was defined elsewhere in the file.
                            targetingDef = nameResolver.resolveSyntaxIdentifier<WCWeaponDefinition.targetingDef>(rightINS.Identifier);
                            //throw new Exception($"Unimplemented!");
                            break;

                        default:
                            throw new Exception($"Unknown syntax {dnAES.Right} in: Targeting.assignment.Right");
                    }
                    break;

            }

        }

        public static void weaponDefParseAssignments(ExpressionSyntax dataNode, ref WCWeaponDefinition.assignmentsDef assignmentsDef)
        {
            // Check the right-hand side of assignments dnExpression 
            switch (dataNode)
            {

                case AssignmentExpressionSyntax dnAES:
                    switch (dnAES.Right)
                    {
                        case ObjectCreationExpressionSyntax dnOCES:
                            // For each assignments definition...
                            foreach (ExpressionSyntax syntaxNode in dnOCES.Initializer.Expressions)
                            {
                                switch (syntaxNode)
                                {

                                    case AssignmentExpressionSyntax dnAES2:

                                        if (dnAES2.Left.ToString().Equals("Scope"))
                                        {
                                            // Create new scopeDef to add to dataNodes.

                                            switch (dnAES2.Right)
                                            {
                                                case LiteralExpressionSyntax dnLES:
                                                    assignmentsDef.scope = dnLES.Token.ToString();
                                                    break;

                                                default:
                                                    throw new Exception("Unimplemented!");
                                            }
                                        }


                                        if (dnAES2.Left.ToString().Equals("MountPoints"))
                                        {
                                            // Create new mountpointDef to add to dataNodes.
                                            WCWeaponDefinition.assignmentsDef.mountPointDef newMountpointDef = new WCWeaponDefinition.assignmentsDef.mountPointDef();

                                            switch (dnAES2.Right)
                                            {
                                                case ImplicitArrayCreationExpressionSyntax dnIACES:

                                                    // foreach mountPointDef in Mountpoints 
                                                    foreach (ExpressionSyntax dnExpression2 in dnIACES.Initializer.Expressions)
                                                    {
                                                        switch (dnExpression2)
                                                        {
                                                            case ObjectCreationExpressionSyntax dnOCES2:
                                                                switch (dnOCES2.Initializer)
                                                                {
                                                                    case InitializerExpressionSyntax dnIES:
                                                                        wcDefAssignMountpointDefs(dnIES.Expressions, ref newMountpointDef);
                                                                        break;
                                                                    default:
                                                                        throw new Exception($"Unknown syntax {dnOCES2.Initializer.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints.dnOCES2.Initializer!");

                                                                }
                                                                break;
                                                            default:
                                                                throw new Exception($"Unknown syntax {dnExpression2.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints.dnExpression2");
                                                        }

                                                    }
                                                    break;
                                                default:
                                                    throw new Exception($"Unknown syntax {dnAES2.Right.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints");
                                            }

                                            assignmentsDef.mountPoints.Add(newMountpointDef);

                                        }

                                        if (dnAES2.Left.ToString().Equals("Muzzles"))
                                        {

                                            switch (dnAES2.Right)
                                            {
                                                case ImplicitArrayCreationExpressionSyntax madIACES:
                                                    foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                    {
                                                        switch (madES)
                                                        {
                                                            case LiteralExpressionSyntax madLES:
                                                                try
                                                                {
                                                                    assignmentsDef.muzzles.Add(madLES.ToString());
                                                                }
                                                                catch
                                                                {
                                                                    Trace.WriteLine($"Failure to change ammoDefinition.assignmentsDefinition.muzzles");
                                                                }
                                                                break;
                                                            default:
                                                                throw new Exception($"Unknown syntax {madES} in: Assignments.ModelAssignmentsDef.Muzzles.dnExpression2");
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    throw new Exception($"Unknown syntax {dnAES2.Right} in: Assignments.ModelAssignmentsDef.Muzzles.dnAES2.Right");
                                            }

                                        }

                                        if (dnAES2.Left.ToString().Equals("Ejector"))
                                        {

                                            switch (dnAES2.Right)
                                            {
                                                case LiteralExpressionSyntax madSLE:
                                                    try
                                                    {
                                                        assignmentsDef.ejectors.Add(madSLE.ToString());
                                                    }
                                                    catch
                                                    {
                                                        Trace.WriteLine($"Failure to change ammoDefinition.assignmentsDefinition.ejectors");
                                                    }
                                                    break;
                                                default:
                                                    throw new Exception($"Unknown syntax {dnAES2.Right} in: Assignments.ModelAssignmentsDef.Ejector.dnAES2.Right");
                                            }
                                        }
                                        break;
                                    case InitializerExpressionSyntax dnIES:
                                        switch (dnOCES.Initializer)
                                        {
                                            case InitializerExpressionSyntax:

                                                foreach (ExpressionSyntax expression in dnOCES.Initializer.Expressions)
                                                {
                                                    switch (expression)
                                                    {
                                                        case AssignmentExpressionSyntax madAES:

                                                            if (madAES.Left.ToString().Equals("MountPoints"))
                                                            {

                                                                switch (madAES.Right)
                                                                {
                                                                    case ImplicitArrayCreationExpressionSyntax madIACES:

                                                                        // foreach mountPointDef in Mountpoints
                                                                        foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                                        {
                                                                            WCWeaponDefinition.assignmentsDef.mountPointDef newMountpointDef = new WCWeaponDefinition.assignmentsDef.mountPointDef();

                                                                            switch (madES)
                                                                            {
                                                                                case ObjectCreationExpressionSyntax madOCES:
                                                                                    switch (madOCES.Initializer)
                                                                                    {
                                                                                        case InitializerExpressionSyntax madIES:
                                                                                            // foreach dnExpression in mountPointDef
                                                                                            wcDefAssignMountpointDefs(madOCES.Initializer.Expressions, ref newMountpointDef);
                                                                                            break;
                                                                                        default:
                                                                                            throw new Exception($"Unknown syntax {madOCES.Initializer.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints.dnOCES2.Initializer!");

                                                                                    }
                                                                                    break;
                                                                                default:
                                                                                    throw new Exception($"Unknown syntax {madES.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints.dnExpression2");
                                                                            }
                                                                        }
                                                                        break;
                                                                    default:
                                                                        throw new Exception($"Unknown syntax {madAES.Right.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints");
                                                                }
                                                            }

                                                            if (madAES.Left.ToString().Equals("Muzzles"))
                                                            {

                                                                switch (madAES.Right)
                                                                {
                                                                    case ImplicitArrayCreationExpressionSyntax madIACES:
                                                                        foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                                        {
                                                                            switch (madES)
                                                                            {
                                                                                case LiteralExpressionSyntax madLES:
                                                                                    try
                                                                                    {
                                                                                        assignmentsDef.muzzles.Add(madLES.ToString());
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        Trace.WriteLine($"Failure to change ammoDefinition.assignmentsDefinition.muzzles");
                                                                                    }
                                                                                    break;
                                                                                default:
                                                                                    throw new Exception($"Unknown syntax {madES} in: Assignments.ModelAssignmentsDef.Muzzles.dnExpression2");
                                                                            }
                                                                        }
                                                                        break;
                                                                    default:
                                                                        throw new Exception($"Unknown syntax {madAES.Right} in: Assignments.ModelAssignmentsDef.Muzzles.dnAES2.Right");
                                                                }

                                                            }

                                                            if (madAES.Left.ToString().Equals("Ejector"))
                                                            {

                                                                switch (madAES.Right)
                                                                {
                                                                    case LiteralExpressionSyntax madSLE:
                                                                        try
                                                                        {
                                                                            assignmentsDef.ejectors.Add(madSLE.ToString());
                                                                        }
                                                                        catch
                                                                        {
                                                                            Trace.WriteLine($"Failure to change ammoDefinition.assignmentsDefinition.ejectors");
                                                                        }
                                                                        break;
                                                                    default:
                                                                        throw new Exception($"Unknown syntax {madAES.Right} in: Assignments.ModelAssignmentsDef.Ejector.dnAES2.Right");
                                                                }
                                                            }
                                                            break;
                                                        default:
                                                            throw new Exception($"Unknown syntax {expression.ToString()} in: Assignments.ModelAssignmentsDef.dnExpression");
                                                    }

                                                }
                                                break;
                                        }

                                        break;
                                    case IdentifierNameSyntax dnINS:

                                        if (dnINS.Identifier.Text.Equals("ModelAssignmentsDef"))
                                        {
                                            switch (dnOCES.Initializer)
                                            {
                                                case InitializerExpressionSyntax:

                                                    foreach (ExpressionSyntax expression in dnOCES.Initializer.Expressions)
                                                    {
                                                        switch (expression)
                                                        {
                                                            case AssignmentExpressionSyntax madAES:

                                                                if (madAES.Left.ToString().Equals("MountPoints"))
                                                                {
                                                                    // Create new mountpointDef to add to dataNodes.
                                                                    WCWeaponDefinition.assignmentsDef.mountPointDef newMountpointDef = new WCWeaponDefinition.assignmentsDef.mountPointDef();

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case ImplicitArrayCreationExpressionSyntax madIACES:

                                                                            // foreach mountPointDef in Mountpoints 
                                                                            foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                                            {
                                                                                switch (madES)
                                                                                {
                                                                                    case ObjectCreationExpressionSyntax madOCES:
                                                                                        switch (madOCES.Initializer)
                                                                                        {
                                                                                            case InitializerExpressionSyntax madIES:

                                                                                                // foreach lvalue=rvalue dnExpression in mountPointDef
                                                                                                foreach (ExpressionSyntax madES2 in madIES.Expressions)
                                                                                                {
                                                                                                    // Create new addition to mountpoints dataNodes
                                                                                                    switch (madES2)
                                                                                                    {
                                                                                                        case AssignmentExpressionSyntax madAES2:

                                                                                                            string left = madAES2.Left.ToString();
                                                                                                            string right = madAES2.Right.ToString();

                                                                                                            switch (left)
                                                                                                            {
                                                                                                                case "SubtypeId":
                                                                                                                    newMountpointDef.subtypeID = right;
                                                                                                                    break;
                                                                                                                case "SpinPartId":
                                                                                                                    newMountpointDef.spinpartID = right;
                                                                                                                    break;
                                                                                                                case "MuzzlePartId":
                                                                                                                    newMountpointDef.muzzlePartID = right;
                                                                                                                    break;
                                                                                                                case "AzimuthPartId":
                                                                                                                    newMountpointDef.azimuthPartID = right;
                                                                                                                    break;
                                                                                                                case "ElevationPartId":
                                                                                                                    newMountpointDef.elevationPartID = right;
                                                                                                                    break;
                                                                                                                case "DurabilityMod":
                                                                                                                    if (right.Contains('f'))
                                                                                                                    {
                                                                                                                        // We remove the right.Length - 1 to remove the 'f' in float literal.
                                                                                                                        newMountpointDef.durabilityMod = float.Parse(right.Remove(right.Length - 1));
                                                                                                                    }
                                                                                                                    else
                                                                                                                    {
                                                                                                                        newMountpointDef.durabilityMod = float.Parse(right);
                                                                                                                    }
                                                                                                                    break;
                                                                                                                case "IconName":
                                                                                                                    newMountpointDef.iconName = right;
                                                                                                                    break;
                                                                                                                default:
                                                                                                                    Trace.WriteLine($"Found unknown mountpoint identifier: {left}");
                                                                                                                    break;
                                                                                                            }
                                                                                                            Trace.WriteLine($"{left} = {right}");
                                                                                                            break;
                                                                                                    }
                                                                                                }
                                                                                                break;
                                                                                            default:
                                                                                                throw new Exception($"Unknown syntax {madOCES.Initializer.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints.dnOCES2.Initializer!");

                                                                                        }
                                                                                        break;
                                                                                    default:
                                                                                        throw new Exception($"Unknown syntax {madES.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints.dnExpression2");
                                                                                }

                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Unknown syntax {madAES.Right.ToString()} in: Assignments.ModelAssignmentsDef.MountPoints");
                                                                    }

                                                                    assignmentsDef.mountPoints.Add(newMountpointDef);

                                                                }

                                                                if (madAES.Left.ToString().Equals("Muzzles"))
                                                                {

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case ImplicitArrayCreationExpressionSyntax madIACES:
                                                                            foreach (ExpressionSyntax madES in madIACES.Initializer.Expressions)
                                                                            {
                                                                                switch (madES)
                                                                                {
                                                                                    case LiteralExpressionSyntax madLES:
                                                                                        try
                                                                                        {
                                                                                            assignmentsDef.muzzles.Add(madLES.ToString());
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            Trace.WriteLine($"Failure to change ammoDefinition.assignmentsDefinition.muzzles");
                                                                                        }
                                                                                        break;
                                                                                    default:
                                                                                        throw new Exception($"Unknown syntax {madES} in: Assignments.ModelAssignmentsDef.Muzzles.dnExpression2");
                                                                                }
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Unknown syntax {madAES.Right} in: Assignments.ModelAssignmentsDef.Muzzles.dnAES2.Right");
                                                                    }

                                                                }

                                                                if (madAES.Left.ToString().Equals("Ejector"))
                                                                {

                                                                    switch (madAES.Right)
                                                                    {
                                                                        case LiteralExpressionSyntax madSLE:
                                                                            try
                                                                            {
                                                                                assignmentsDef.ejectors.Add(madSLE.ToString());
                                                                            }
                                                                            catch
                                                                            {
                                                                                Trace.WriteLine($"Failure to change ammoDefinition.assignmentsDefinition.ejectors");
                                                                            }
                                                                            break;
                                                                        default:
                                                                            throw new Exception($"Unknown syntax {madAES.Right} in: Assignments.ModelAssignmentsDef.Ejector.dnAES2.Right");
                                                                    }
                                                                }
                                                                break;
                                                            default:
                                                                throw new Exception($"Unknown syntax {expression.ToString()} in: Assignments.ModelAssignmentsDef.dnExpression");
                                                        }

                                                    }
                                                    break;
                                                default:
                                                    throw new Exception($"Unknown syntax {syntaxNode} in: Assignments.ModelAssignmentsDef.dnOCES.Initializer");
                                            }
                                        }
                                        break;
                                    default:
                                        throw new Exception($"Unknown syntax {syntaxNode} in: Assignments.ModelAssignmentsDef.syntaxNode");
                                }
                            }
                            break;

                    }
                    break;
                default:
                    throw new Exception($"Unknown syntax {dataNode} in: Assignments.assignment.Right");
            }
        }

        public static void wcDefAssignMountpointDefs(SeparatedSyntaxList<SyntaxNode> dataNodes, ref WCWeaponDefinition.assignmentsDef.mountPointDef mountpointDef)
        {
            // foreach lvalue=rvalue dnExpression in mountPointDef
            foreach (ExpressionSyntax mountpointAssignment in dataNodes)
            {
                // Create new addition to mountpoints dataNodes
                switch (mountpointAssignment)
                {
                    case AssignmentExpressionSyntax assignmentAES:

                        string left = assignmentAES.Left.ToString();
                        string right = assignmentAES.Right.ToString();

                        switch (left)
                        {
                            case "SubtypeId":
                                mountpointDef.subtypeID = right;
                                break;
                            case "SpinPartId":
                                mountpointDef.spinpartID = right;
                                break;
                            case "MuzzlePartId":
                                mountpointDef.muzzlePartID = right;
                                break;
                            case "AzimuthPartId":
                                mountpointDef.azimuthPartID = right;
                                break;
                            case "ElevationPartId":
                                mountpointDef.elevationPartID = right;
                                break;
                            case "DurabilityMod":
                                if (right.Contains('f'))
                                {
                                    // We remove the right.Length - 1 to remove the 'f' in float literal.
                                    mountpointDef.durabilityMod = float.Parse(right.Remove(right.Length - 1));
                                }
                                else
                                {
                                    mountpointDef.durabilityMod = float.Parse(right);
                                }
                                break;
                            case "IconName":
                                mountpointDef.iconName = right;
                                break;
                            default:
                                Trace.WriteLine($"Found unknown mountpoint identifier: {left}");
                                break;
                        }

                        Trace.WriteLine($"{left} = {right}");
                        break;
                    default:
                        throw new Exception($"Unknown syntax {mountpointAssignment.ToString()} in: wcDefAssignMountpointDefs");
                }
            }
        }

        public static void weaponDefParseAmmos(ExpressionSyntax dataNode, ref List<string> ammoDefsList)
        {
            switch (dataNode)
            {
                case AssignmentExpressionSyntax dnAES:

                    switch (dnAES.Right)
                    {
                        case ImplicitArrayCreationExpressionSyntax madIACE:

                            foreach (ExpressionSyntax madES in madIACE.Initializer.Expressions)
                            {
                                switch (madES)
                                {
                                    case IdentifierNameSyntax madINS:
                                        ammoDefsList.Add(madINS.ToString());
                                        break;
                                    case AssignmentExpressionSyntax madAES:
                                        Trace.WriteLine("Found ammo assignment " + madAES.Left.ToString() + "\n");
                                        break;
                                    default:
                                        throw new Exception($"Unknown syntax {madES} in: Ammos.dnExpression2");
                                }
                            }
                            break;
                        default:
                            throw new Exception($"Unknown syntax {dnAES.Right} in: Ammos.assignment.Right");
                    }
                    break;
            }
        }
        /*Syntaxwalker for weapon definitions*/
        public class WCWeaponWalker : CSharpSyntaxWalker
        {
            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                // These variables are intended to be used in the iterations below.
                // Feed their values into the ammoDefinition!
                //WCWeaponDefinition.assignmentsDef.mountPointDef mountPointDef = new WCWeaponDefinition.assignmentsDef.mountPointDef();

                try
                {
                    if (node.Type.ToString().Equals("WeaponDefinition"))
                    {
                        WCWeaponDefinition weaponDefinition = new WCWeaponDefinition();


                        // To get the definitionName, we have to go the 'up' the tree first.
                        switch (node.Parent)
                        {
                            case ArrowExpressionClauseSyntax aecs:
                                switch (aecs.Parent)
                                {
                                    case PropertyDeclarationSyntax pds:
                                        weaponDefinition.definitionName = pds.Identifier.ToString();
                                        nameResolver.addIdentifierAndDefinition(pds.Identifier, weaponDefinition);
                                        break;

                                    default:
                                        break;
                                }
                                break;

                            default:
                                throw new Exception($"Unknown sytax {node.Parent} in WeaponDefinition!");
                        }

                        // Now we go 'down' the tree to find/make our assignments
                        if (node.Initializer != null)
                        {
                            // For every definition in the ammoDefinition...
                            foreach (AssignmentExpressionSyntax assignment in node.Initializer.Expressions)
                            {
                                // Found the Assignments dnExpression
                                if (assignment.Left.ToString().Equals("Assignments"))
                                {
                                    Trace.WriteLine("Found Assignments dnExpression!\n");

                                    weaponDefParseAssignments(assignment, ref weaponDefinition.assignmentsDefinition);
                                }

                                // Found the Targeting dnExpression
                                if (assignment.Left.ToString().Equals("Targeting"))
                                {
                                    Trace.WriteLine("Found Targeting dnExpression!\n");
                                    weaponDefParseTargeting(assignment, ref weaponDefinition.targetingDefinition);

                                }

                                // Found the HardPoint dnExpression
                                if (assignment.Left.ToString().Equals("HardPoint"))
                                {
                                    Trace.WriteLine("Found HardPoint dnExpression!\n");
                                    weaponDefParseHardpoint(assignment, ref weaponDefinition.hardpointDefinition);
                                }

                                // Found the Ammos dnExpression
                                if (assignment.Left.ToString().Equals("Ammos"))
                                {

                                    Trace.WriteLine("Found Ammos dnExpression!\n");
                                    weaponDefParseAmmos(assignment, ref weaponDefinition.ammos);
                                }

                                // Found the Animations dnExpression
                                if (assignment.Left.ToString().Equals("Animations"))
                                {

                                    Trace.WriteLine("Found Animations dnExpression!\n");
                                    switch (assignment.Right)
                                    {
                                        case IdentifierNameSyntax madINS:
                                            weaponDefinition.animations.Add(madINS.ToString());
                                            break;
                                        default:
                                            throw new Exception($"Unknown syntax {assignment.Right} in: Ammos.assignment.Right");
                                    }
                                }
                            }
                        }

                        // Finally: We add our definition to the dataNodes of definitions
                        wcDefinitions.Add(weaponDefinition);
                    }
                    else
                    {
                        Trace.WriteLine("Found Non-Weapon definition! Skipping!");
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error at {DateTime.Now.ToShortTimeString()}: \n{ex.ToString()}\n");
                }



                base.VisitObjectCreationExpression(node);


            }
        }

        /*Syntaxwalker for weapon definitions*/
        public class WCAmmoWalker : CSharpSyntaxWalker
        {
            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                // These variables are intended to be used in the iterations below.
                // Feed their values into the ammoDefinition!
                //WCWeaponDefinition.assignmentsDef.mountPointDef mountPointDef = new WCWeaponDefinition.assignmentsDef.mountPointDef();

                try
                {
                    if (node.Type.ToString().Equals("AmmoDef"))
                    {
                        WCAmmoDefinition ammoDefinition = new WCAmmoDefinition();


                        // To get the definitionName, we have to go the 'up' the tree first.
                        switch (node.Parent)
                        {
                            case ArrowExpressionClauseSyntax aecs:
                                switch (aecs.Parent)
                                {
                                    case PropertyDeclarationSyntax pds:
                                        //ammoDefinition.definitionName = pds.Identifier.ToString();
                                        nameResolver.addIdentifierAndDefinition(pds.Identifier, ammoDefinition);
                                        break;

                                    default:
                                        break;
                                }
                                break;

                            default:
                                throw new Exception($"Unknown sytax {node.Parent} in WeaponDefinition!");
                        }

                        // Now we go 'down' the tree to find/make our assignments
                        if (node.Initializer != null)
                        {
                            SeparatedSyntaxList<ExpressionSyntax> expressionList = node.Initializer.Expressions;
                            ExpressionSyntax test;

                            //debug thing
                            // For every definition in the ammoDefinition...
                            foreach (ExpressionSyntax expression in expressionList)
                            {
                                Trace.WriteLine($"Expression is: {expression}");
                                switch (expression)
                                {
                                    case AssignmentExpressionSyntax assignment:
                                        {
                                            if (assignment.Left.ToString().Equals("AmmoMagazine"))
                                            {
                                                ammoDefinition.ammoMagazine = assignment.Right.ToString();
                                            }

                                            if (assignment.Left.ToString().Equals("AmmoRound"))
                                            {
                                                ammoDefinition.ammoRound = assignment.Right.ToString();
                                            }

                                            if (assignment.Left.ToString().Equals("HybridRound"))
                                            {
                                                ammoDefinition.hybridRound = Boolean.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("EnergyCost"))
                                            {
                                                ammoDefinition.energyCost = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("BaseDamage"))
                                            {
                                                ammoDefinition.baseDamage = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("_Mass"))
                                            {
                                                ammoDefinition.mass = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("_Health"))
                                            {
                                                ammoDefinition.health = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("BackKickForce"))
                                            {
                                                ammoDefinition.backKickForce = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("DecayPerShot"))
                                            {
                                                ammoDefinition.decayPerShot = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("HardPointUsable"))
                                            {
                                                ammoDefinition.hardPointUsable = Boolean.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("EnergyMagazineSize"))
                                            {
                                                ammoDefinition.energyMagazineSize = float.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("IgnoreWater"))
                                            {
                                                ammoDefinition.ignoreWater = Boolean.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("IgnoreVoxels"))
                                            {
                                                ammoDefinition.ignoreVoxels = Boolean.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("Synchronize"))
                                            {
                                                ammoDefinition.synchronize = Boolean.Parse(assignment.Right.ToString());
                                            }

                                            if (assignment.Left.ToString().Equals("HeatModifier"))
                                            {
                                                ammoDefinition.heatModifier = float.Parse(assignment.Right.ToString());
                                            }


                                            if (assignment.Left.ToString().Equals("Shape"))
                                            {
                                                ammoDefinition.shapeDef = new WCAmmoDefinition.shapeDefinition();
                                            }

                                            if (assignment.Left.ToString().Equals("ObjectsHit"))
                                            {
                                                ammoDefinition.objectsHit = new WCAmmoDefinition.objectsHitDefinition();
                                            }

                                            if (assignment.Left.ToString().Equals("Fragment"))
                                            {
                                                ammoDefinition.fragmentsDef = new WCAmmoDefinition.fragmentsDefinition();
                                            }

                                            if (assignment.Left.ToString().Equals("Pattern"))
                                            {
                                                ammoDefinition.patternDef = new WCAmmoDefinition.patternDefinition();
                                            }

                                            if (assignment.Left.ToString().Equals("DamageScales"))
                                            {
                                                ammoDefinition.damageScalesDef = new WCAmmoDefinition.DamageScales();
                                            }

                                            if (assignment.Left.ToString().Equals("AreaOfDamage"))
                                            {
                                                ammoDefinition.areaOfDamageDef = new WCAmmoDefinition.AreaOfDamageDef();
                                            }

                                            if (assignment.Left.ToString().Equals("Ewar"))
                                            {
                                                ammoDefinition.ewarDefinition = new WCAmmoDefinition.ewarDef();
                                            }

                                            if (assignment.Left.ToString().Equals("Beams"))
                                            {
                                                ammoDefinition.beamDef = new WCAmmoDefinition.BeamDef();
                                            }

                                            if (assignment.Left.ToString().Equals("Trajectory"))
                                            {
                                                ammoDefinition.trajectoryDef = new WCAmmoDefinition.TrajectoryDef();
                                            }


                                            if (assignment.Left.ToString().Equals("AmmoGraphics"))
                                            {
                                                ammoDefinition.ammoGraphicsDefinition = new WCAmmoDefinition.ammoGraphicsDef();
                                            }


                                            if (assignment.Left.ToString().Equals("AmmoAudio"))
                                            {
                                                ammoDefinition.ammoAudioDefinition = new WCAmmoDefinition.ammoAudioDef();
                                            }

                                            if (assignment.Left.ToString().Equals("Ejection"))
                                            {
                                            }
                                            break;
                                        }

                                    default:
                                        Trace.WriteLine("Hit default!");
                                        break;
                                }
                            }

                        }

                        // Finally: We add our definition to the dataNodes of definitions
                        wcDefinitions.Add(ammoDefinition);
                    }
                    else
                    {
                        Trace.WriteLine("Found Non-Weapon definition! Skipping!");
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error at {DateTime.Now.ToShortTimeString()}: \n{ex.ToString()}\n");
                }



                base.VisitObjectCreationExpression(node);


            }
        }

        public ScottPlot.Plottables.Marker maxDistMarker(float x, float y, string label, ScottPlot.Color color)
        {
            ScottPlot.Plottables.Marker result = new ScottPlot.Plottables.Marker();

            result.X = x;
            result.Y = y;
            result.Size = 20;
            result.Label = label;
            result.Shape = MarkerShape.TriUp;
            result.Color = color;

            result.MarkerStyle.Shape = MarkerShape.TriUp;
            result.MarkerStyle.Size = 20;
            result.MarkerStyle.OutlineColor = color;
            return result;
        }

        public static T assignDefaultOrValue<T>(T value)
        {
            T result = default(T);

            if (value != null)
            {
                result = value;
                return result;
            }
            else
            {
                return result;
            }
        }

    }
}