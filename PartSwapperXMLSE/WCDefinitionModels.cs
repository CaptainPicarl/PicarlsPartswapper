using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PartSwapperXMLSE
{


    public interface I_WCDefinition
    {
        // Marker interface

    }
    public class particleDef : I_WCDefinition
    {
        public string name;
        public bool applyToShield;
        public Color color;
        public Vector3 offset;
        public particleOptionDef extras;

        public particleDef()
        {
            this.name = "unnamedParticleDef";
            this.color = new Color();
            this.offset = new Vector3();
            this.extras = new particleOptionDef();
        }

        public class particleOptionDef : I_WCDefinition
        {
            public bool loop;
            public bool restart;
            public float maxDistance;
            public float maxDuration;
            public float scale;
            public float hitPlayChance;
        }
    }

    public class WCArmorDefinition() : I_WCDefinition
    {
        public List<string>? subtypeIDs { set; get; }
        public float? EnergeticResistance { set; get; }
        public float? KineticResistance { set; get; }
        public string? kind { set; get; }

    }

    // TODO: As of 28FEB2024, I have decided to do the hard thing and *sigh*...recreate definition objects after parsing them via roslyn.
    // Define the classes that model the information we are interested in below.

    public class WCAmmoDefinition() : I_WCDefinition
    {
        public string? ammoMagazine;
        public string? ammoRound;
        public bool? hybridRound;
        public float? energyCost;
        public float? baseDamage;
        public float? backKickForce;
        public float? decayPerShot;
        public float? mass;
        public float? health;
        public float? energyMagazineSize;
        public bool? hardPointUsable;
        public bool? ignoreWater;
        public bool? ignoreVoxels;
        public bool? synchronize;
        public float? heatModifier;

        public shapeDefinition? shapeDef;
        public objectsHitDefinition? objectsHit;
        public fragmentsDefinition? fragmentsDef;
        public patternDefinition? patternDef;
        public DamageScales? damageScalesDef;
        public AreaOfDamageDef? areaOfDamageDef;
        public ewarDef? ewarDefinition;
        public BeamDef? beamDef;
        public TrajectoryDef? trajectoryDef;
        public ammoGraphicsDef? ammoGraphicsDefinition;
        public ammoAudioDef? ammoAudioDefinition;
        
        public class objectsHitDefinition()
        {
            public float maxObjectsHit;
            public bool countBlocks;
        }

        public class fragmentsDefinition()
        {
            public string ammoRound;
            public float fragments;
            public float degrees;
            public bool reverse;
        }

        public class patternDefinition()
        {
            public List<string> patterns;
            public bool enable;
            public float triggerChance;
            public bool random;
            public float randomMin;
            public float randomMax;
            public bool skipParent;
            public float patternSteps;
        }

        public class shapeDefinition()
        {
            public string shape;
            public float diameter;
        }







        public class DamageScales()
        {
            public bool damageVoxels;
            public bool selfDamage;

            public float maxIntegrity;
            public float healthHitModifier;
            public float voxelHitModifier;
            public float charactersDamageMult;

            public fallOffDef fallOff;
            public gridSizeDef grids;
            public armorDef armor;
            public shieldDef shields;
            public damageType damageTypeDef;
            public customScalesDef custom;

            public class fallOffDef()
            {
                public float distance;
                public float minMultiplier;
            }

            public class gridSizeDef()
            {
                public float largeGridDamageMult;
                public float smallGridDamageMult;
            }

            public class armorDef()
            {
                public float armorDamageMult;
                public float lightDamageMult;
                public float heavyDamageMult;
                public float nonArmorDamageMult;
            }

            public class shieldDef()
            {
                public float modifier;
                public string type = "";
                public float bypassModifier;
            }

            public class damageType()
            {
                public string baseDamageType;
                public string areaEffect;
                public string detonation;
                public string shield;
            }

            public class customScalesDef()
            {
                public bool ignoreAllOthers;
                public List<customBlocksDef> customBlocksDefs;

                public class customBlocksDef()
                {
                    public string subtypeID;
                    public float modifier;
                }
            }
        }


        public class AreaOfDamageDef()
        {
            public class ByBlockHit()
            {
                public bool enable;
                public float radius;
                public float damage;
                public float depth;
                public float maxAbsorb;
                public string falloff = "";
                public string shape = "";
            }

            public class EndOfLife()
            {
                public bool enable;
                public float radius;
                public float damage;
                public float depth;
                public float maxAbsorb;
                public string falloff = "";
                public bool armOnlyOnHit;
                public float minAArmingTime;
                public bool noVisuals;
                public bool noSound;
                public float particaleScale;
                public string shape = "";
            }

        }


        public class ewarDef()
        {
            public bool enable;
            public string type;
            public string mode;
            public float strength;
            public float radius;
            public float duration;
            public bool stackDuration;
            public bool depletable;
            public float maxStacks;
            public bool noHitParticle;

            // Might need to make a "Force" object and subclass it in the future...
            public pushPullDef Force;

            public class pushPullDef()
            {
                public string forceFrom;
                public string forceeTo;
                public string position;
                public bool disableRelativeMass;
                public float tractorRange;
                public bool shooterFeelsForce;
            }
        }
        public class BeamDef()
        {
            public bool enable;
            public bool virtualBeams;
            public bool rotateRealBeam;
            public bool oneParticle;
        }

        public class TrajectoryDef()
        {
            public string guidance;
            public float targetLossDegree;
            public float targetLossTime;
            public float maxLifeTime;
            public float accelPerSec;
            public float desiredSpeed;
            public float maxTrajectory;
            public float gravityMultiplier;
            public Range speedVarianceRange;
            public Range rangeVarianceRange;
            public float MaxTrajectoryTime;
        }

        public class ammoGraphicsDef()
        {
            public string modelPath = "";
            public float visualProbability;
            public bool shieldHitDraw;
            public decalsDef decalDef;
            public ammoParticleDef ammoParticleDefinition;
            public linesDef lineDefinition;

            public class decalsDef()
            {
                public float maxAge;
                public List<textureMapDef> map;

                public class textureMapDef()
                {
                    public string hitMaterial = "";
                    public string decalMaterial = "";
                }
            }

            public class ammoParticleDef()
            {
                public particleDef ammo;
                public particleDef hit;
                public particleDef eject;

            }

            public class linesDef()
            {
                public Range colorVariance;
                public Range widthVariance;
                public tracerBaseDef tracerBaseDefinition;
                public trailDef trailDefinition;
                public offsetEffectDef offsetEffectDefinition;

                public class trailDef()
                {
                    public bool enable;
                    public List<string> textures;
                    public string textureMode;
                    public float decayTime;
                    public Color color;
                    public bool back;
                    public float customWidth;
                    public bool useWidthVariance;
                    public bool useColorFade;

                }
                public class offsetEffectDef()
                {

                    public float maxOffset;
                    public float minLength;
                    public float maxLength;
                }

                public class tracerBaseDef()
                {
                    public bool enable;
                    public float length;
                    public Color color;
                    public float visualFadeStart;
                    public float visualFadeEnd;
                    public List<string> textures;
                    public string textureMode;

                    public class segmentDef()
                    {
                        public bool enable;
                        public List<string> textures;
                        public float segmentLength;
                        public float segmentGap;
                        public float speed;
                        public Color color;
                        public float widthMultiplier;
                        public bool reverse;
                        public bool useLineVariance;
                        public Range widthVariance;
                        public Range colorVariance;
                    }



                }
            }
        }

        public class ammoAudioDef()
        {
            public string travelSound;
            public string hitSound;
            public string shotSound;
            public string shieldHitSound;
            public string playerHitSound;
            public string voxelHitSound;
            public string floatingHitSound;
            public float hitPlayChance;
            public bool hitPlayShield;
        }

        public class ejectionDef()
        {
            public string particle;
            public float speed;
            public float spawnChance;
            public componentDef componentDefinition;

            public class componentDef()
            {
                public string itemName;
                public float itemLifeTime;
                public float delay;
            }
        }
        public class SmartsDef()
        {
            public float inaccuracy;
            public float aggressiveness;
            public float maxLateralThrust;
            public float trackingDelay;
            public float maxChaseTime;
            public bool overrideTarget;
            public float maxTargets;
            public bool noTargetExpire;
            public bool roam;
            public bool keepAliveAfterTaargetLoss;
            public float offsetRatio;
            public float offsetTime;
        }

        public class MinesDef()
        {
            public float detectRadius;
            public float deCloakRadius;
            public float fieldTime;
            public bool cloak;
            public bool persist;
        }

    }

    public class WCWeaponDefinition : I_WCDefinition
    {
        public string definitionName;
        public assignmentsDef assignmentsDefinition;
        public targetingDef targetingDefinition;
        public hardpointDef hardpointDefinition;
        public List<string> ammos;
        public List<string> animations;

        public WCWeaponDefinition()
        {
            this.definitionName = "";
            this.targetingDefinition = new targetingDef();
            this.hardpointDefinition = new hardpointDef();
            this.assignmentsDefinition = new assignmentsDef();
            this.ammos = new List<string>();
            this.animations = new List<string>();
        }

        public class assignmentsDef : I_WCDefinition
        {
            public List<mountPointDef> mountPoints;
            public List<string> muzzles;
            public List<string> ejectors;
            public string scope;

            public assignmentsDef()
            {
                mountPoints = new List<mountPointDef>();
                muzzles = new List<string>();
                ejectors = new List<string>();
                scope = "";
            }

            public class mountPointDef : I_WCDefinition
            {
                public string? subtypeID;
                public string? spinpartID;
                public string? muzzlePartID;
                public string? azimuthPartID;
                public string? elevationPartID;
                public string? iconName;
                public float? durabilityMod;
            }

            // 14AAPR2024: Currently unused, as muzzles seem to just be strings
            public class muzzlesDef : I_WCDefinition
            {
                public List<string>? muzzles;

                public muzzlesDef()
                {
                    muzzles = new List<string>();
                }
            }

            // 14AAPR2024: Currently unused, as ejectors seem to just be strings
            public class ejectorsDef : I_WCDefinition
            {
                public List<string>? ejectors;

                public ejectorsDef()
                {
                    this.ejectors = new List<string>();
                }
            }
        }
        public class targetingDef : I_WCDefinition
        {
            public List<string> threats;
            public List<string> subsystems;
            public bool closestFirst;
            public bool ignoreDumbProjectiles;
            public bool lockedSmartOnly;
            public float minimumDiameter;
            public float maximumDiameter;
            public float maxTargetDistance;
            public float minTargetDistance;
            public float topTargets;
            public float topBlocks;
            public float stopTrackingSpeed;

            public targetingDef()
            {
                this.threats = new List<string>();
                this.subsystems = new List<string>();
            }
        }
        public class hardpointDef : I_WCDefinition
        {
            public string partName;
            public string aimLeadingPrediction;
            public float deviateShotAngle;
            public float aimingTolerance;
            public float delayCeaseFire;
            public bool addToleranceToTracking;
            public bool canShootSubmerged;
            public uiDef uiDefinition;
            public aiDef aiDefinition;
            public hardwareDef hardwareDefinition;
            public otherDef otherDefinition;
            public loadingDef loadingDefinition;
            public audioDef audioDefinition;
            public hardpointParticleDef graphicsDefinition;

            public hardpointDef()
            {
                this.partName = "unnamedHardpointDef";
                this.aimLeadingPrediction = "";
                this.uiDefinition = new uiDef();
                this.aiDefinition = new aiDef();
                this.hardwareDefinition = new hardwareDef();
                this.otherDefinition = new otherDef();
                this.loadingDefinition = new loadingDef();
                this.audioDefinition = new audioDef();
                this.graphicsDefinition = new hardpointParticleDef();
            }

            public class hardpointParticleDef : I_WCDefinition
            {
                public List<particleDef> particleDefinitions;

                public hardpointParticleDef()
                {
                    particleDefinitions = new List<particleDef>();

                }

            }

            public class audioDef : I_WCDefinition
            {
                public string preFiringSound;
                public string firingSound;
                public bool firingSoundPerShot;
                public string reloadSound;
                public string noAmmoSound;
                public string hardpointRotationSound;
                public string barrelrotationSound;
                public float fireSoundEndDelay;

            }

            public class uiDef : I_WCDefinition
            {
                public bool rateOfFire;
                public bool damageModifier;
                public bool toggleGuidance;
                public bool enableOverload;
            }

            public class aiDef : I_WCDefinition
            {
                public bool trackTargets;
                public bool turretAttached;
                public bool turretController;
                public bool primaryTracking;
                public bool lockOnFocus;
                public bool suppressFire;
                public bool overrideLeads;
            }

            public class otherDef : I_WCDefinition
            {
                public float constructPartCap;
                public float rotateBarrelAxis;
                public float energyPriority;
                public bool muzzleCheck;
                public bool debug;
                public float restrictionRadius;
                public bool checkInflatedBox;
                public bool checkForAnyWeapon;
            }

            public class loadingDef : I_WCDefinition
            {
                public float rateOfFire;
                public float barrelSpinRate;
                public float barrelsPerShot;
                public float trajectilesPerBarrel;
                public float skipBaarrels;
                public float reloadTime;
                public float delayUntilFire;
                public float heatPerShot;
                public float maxHeat;
                public float cooldown;
                public float heatsinkRate;
                public float shotsInBurst;
                public float delayAfterBurst;
                public bool degradeRof;
                public bool fireFull;
                public bool giveUpAfter;
                public bool stayCharged;
            }
            public class hardwareDef : I_WCDefinition
            {
                public float rotateRate;
                public float elevateRate;
                public float minAzimuth;
                public float maxAzimuth;
                public float minElevation;
                public float maxElevation;
                public float homeAzimuth;
                public float homeElevation;
                public float inventorySize;
                public float idlePower;
                public bool fixedOffset;
                public Vector3 offset;
                public string type;
                public critialDef criticalReaction;

                public hardwareDef()
                {
                    this.criticalReaction = new critialDef();
                    this.offset = new Vector3();
                    this.type = "unnamedHardwareDefType";
                }

                public class critialDef : I_WCDefinition
                {
                    public bool enable;
                    public float defaultArmedTimer;
                    public bool preArmed;
                    public bool terminalControls;

                    public critialDef()
                    {
                        // intentionally empty. default 0's work.
                    }
                }

            }

        }
    }
    // VanillaAmmo/Weaponstats were created before I recognized I was not appropriately modeling WC definitions
    // To be clear: As of 24APR2024 they are UNFINISHED and SHOULD NOT BE USED! TODO: Finish VanillaStats
    public class VanillaAmmoStats
    {
        public string typeID;
        public string subTypeID;

        public Dictionary<string, string> basicProperties;
        public Dictionary<string, string> missileProperties;
        public Dictionary<string, string> projectileProperties;

        public VanillaAmmoStats()
        {
            this.typeID = string.Empty;
            this.subTypeID = string.Empty;
            this.basicProperties = new Dictionary<string, string>();
            this.missileProperties = new Dictionary<string, string>();
            this.projectileProperties = new Dictionary<string, string>();
        }

        public VanillaAmmoStats(XElement ammoNode)
        {
            this.typeID = ammoNode.Element("Id").Element("_TypeID").Value;
            this.subTypeID = ammoNode.Element("Id").Element("SubtypeId").Value;

            this.basicProperties = new Dictionary<string, string>();
            this.missileProperties = new Dictionary<string, string>();
            this.projectileProperties = new Dictionary<string, string>();

            if (ammoNode.Element("Basic Properties") != null)
            {
                foreach (XElement basicProperty in ammoNode.Element("Basic Properties").Elements())
                {
                    // Add each basicProperty to the dictionary
                    basicProperties.Add(basicProperty.Name.ToString(), basicProperty.Value);
                }
            }

            if (ammoNode.Element("MissileProperties") != null)
            {
                foreach (XElement missileProperty in ammoNode.Element("MissileProperties").Elements())
                {
                    // Add each missileProperty to the dictionary
                    missileProperties.Add(missileProperty.Name.ToString(), missileProperty.Value);
                }
            }

            if (ammoNode.Element("ProjectileProperties") != null)
            {
                foreach (XElement projectileProperty in ammoNode.Element("ProjectileProperties").Elements())
                {
                    // Add each projectileProperty to the dictionary
                    projectileProperties.Add(projectileProperty.Name.ToString(), projectileProperty.Value);
                }
            }
        }

        public Dictionary<string, string> getBasicProperties()
        {
            return this.basicProperties;
        }

        public Dictionary<string, string> getMissileProperties()
        {
            return this.missileProperties;
        }

        public Dictionary<string, string> getProjectileProperties()
        {
            return this.projectileProperties;
        }
    }

    public class VanillaWeaponStats
    {
        string typeID;
        string subTypeID;

        Dictionary<string, string> projectileAmmoData;
        Dictionary<string, string> ammoMagazines;
        Dictionary<string, string> otherStats;

        public VanillaWeaponStats()
        {
            this.typeID = string.Empty;
            this.subTypeID = string.Empty;

            this.projectileAmmoData = new Dictionary<string, string>();
            this.ammoMagazines = new Dictionary<string, string>();
            this.otherStats = new Dictionary<string, string>();
        }

        public VanillaWeaponStats(XElement weaponNode)
        {
            this.typeID = weaponNode.Element("Id").Element("_TypeID").Value;
            this.subTypeID = weaponNode.Element("Id").Element("SubtypeId").Value;

            this.projectileAmmoData = new Dictionary<string, string>();
            this.ammoMagazines = new Dictionary<string, string>();
            this.otherStats = new Dictionary<string, string>();

            // Skip Id/AmmoMag/ProjectileAmmoData, since we'll handle them later.
            foreach (XElement node in weaponNode.Elements())
            {
                if (node.Name.Equals("Id") || node.Name.Equals("AmmoMagazines") || node.Name.Equals("ProjectileAmmoData"))
                {
                    continue;
                }


            }


            if (weaponNode.Element("ProjectileAmmoData") != null)
            {
                foreach (XAttribute attr in weaponNode.Element("ProjectileAmmoData").Attributes())
                {
                    // Add each basicProperty to the dictionary
                    projectileAmmoData.Add(attr.Name.ToString(), attr.Value);
                }
            }

            if (weaponNode.Element("AmmoMagazines") != null)
            {
                foreach (XElement ammoMagazine in weaponNode.Element("AmmoMagazines").Elements())
                {
                    // Add each missileProperty to the dictionary
                    ammoMagazines.Add(ammoMagazine.Attribute("Subtype").Name.ToString(), ammoMagazine.Attribute("Subtype").Value);
                }
            }
        }
    }

}
