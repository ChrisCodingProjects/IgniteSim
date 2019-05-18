using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace IgniteSim
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.WriteLine("Starting Ignite Simulation");
            TargetDummy target = new TargetDummy();
            Mage mages = new Mage(4, 35, 95, 0, target);
            int tickSize = mages.TimeBetweenFireballs();
            Console.WriteLine("Time Between Fireballs: " + tickSize);
            long numberOfFireballsVolleys = 30000000;
            long maxTicks = numberOfFireballsVolleys * 300;

            for(long tick = 0; tick < maxTicks; tick += tickSize)
            {
                mages.CastFireBall(tick);
            }

            Random temp = new Random();
            FireBall fireball = new FireBall(temp, 35, 95, 0, 0);
            fireball.critStatus = false;
            Console.WriteLine("Single Fireball Dmg (non crit):  " + fireball.damage);
            List<double> report = target.ReportDamage();
            Console.WriteLine("Reports:");
            //Console.WriteLine("Number of spells non-crit: " + report[3]);
            //Console.WriteLine("Number of spells resisted: " + report[4]);
            //Console.WriteLine("Number of spells crit: " + report[5]);
            //Console.WriteLine("Number of total fireballs: " + report[6]);
            Console.WriteLine("Fireball damage taken / 500: " + report[0]/500);
            Console.WriteLine("Ignite damage taken / 500: " + report[1]/500);
            Console.WriteLine("Total damage taken / 500: " + report[2]/500);

            stopWatch.Stop();
            Console.WriteLine("Total Program Timer:       " + stopWatch.ElapsedMilliseconds + " ms");
        }
    }

    class Mage
    {
        public Mage(int NumberOfMages, int CritChance, int HitChance, int SpellPower, TargetDummy Target)
        {
            numberOfMages = NumberOfMages;
            critChance = CritChance;
            hitChance = HitChance;
            spellPower = SpellPower;
            target = Target;
            rand = new Random();
        }
        public int numberOfMages {get; set;}
        public int critChance {get; set;}
        public int hitChance {get; set;}
        public int spellPower {get; set;}
        private TargetDummy target;
        private Random rand;

        public int TimeBetweenFireballs()
        {
            double passTime = (4 / ( numberOfMages * (4d/3d)));
            passTime = Math.Round(passTime, 2) * 100;
            return (int) passTime;
        }
        public void CastFireBall(long tick)
        {
            FireBall fireball = new FireBall(rand, critChance, hitChance, spellPower, tick);
            target.RecieveFireball(fireball);
        }
    }

    class FireBall
    {
        public FireBall(Random Rand, int CritChance, int HitChance, int SpellPower, long Tick)
        {
            rand = Rand;
            hitStatus = Roll(HitChance);
            critStatus = Roll(CritChance);
            damage = GetFireballDamage(SpellPower);
            tick = Tick;
        }
        public bool hitStatus {get; private set;}
        public bool critStatus {get; set;}
        public double damage {get; private set;}
        public long tick {get; private set;}
        private Random rand;

        private bool Roll(int chance)
        {
            // Rolls 1-100
            int roll = rand.Next(1,101);

            if(roll <= chance)
                return true;
            else
                return false;
        }
        private double GetFireballDamage(int spellPower)
        {
            int baseDamage = 678;
            // Spell Coefficeient is 1.0 because fireball is a 3.5 second spell
            int spellCoeffient = 1;

            // Fire Power = 10%
            // Curse of Elements = 10%
            // Improved Scorch = 15%
            // Not counting Nightfall
            double damageMultiplier = 1.1d * 1.1d * 1.15d; //  1.3915
            double fireballDamage = ((baseDamage + (spellPower * spellCoeffient)) * damageMultiplier);
            if(critStatus == true)
            {
                fireballDamage = fireballDamage * 1.5d;
            }
            
            return fireballDamage;
        }
    }

    class TargetDummy
    {
        public TargetDummy()
        {
            ignite = new Ignite();
            fireballDamageTaken = 0;
            igniteDamageTaken = 0;
        }

        public Ignite ignite;
        private double fireballDamageTaken;
        private double igniteDamageTaken;
        private double totalDamageTaken;
       // private int resistCount = 0;
        //private int nonCritCount = 0;
        //private int critCount = 0;

        public void RecieveFireball(FireBall fireball)
        {
            ignite.CheckStatus(fireball.tick);
            if(fireball.hitStatus == false)
            {
                //resistCount++;
                //Console.WriteLine("Tick: " + fireball.tick + " Fireball resisted");
                return;
            }
            else if(fireball.critStatus == true)
            {
                //Console.WriteLine("Tick: " + fireball.tick + " Fireball crit for " + fireball.damage);
                //critCount++;
                ignite.newCrit(fireball.tick, fireball.damage);
                fireballDamageTaken += fireball.damage;
            }
            else
            {
                //Console.WriteLine("Tick: " + fireball.tick + " Fireball hit for " + fireball.damage);
                //nonCritCount++;
                fireballDamageTaken += fireball.damage;
            }
        }
        public List<double> ReportDamage()
        {
            igniteDamageTaken = ignite.ReportIgnites();
            totalDamageTaken = fireballDamageTaken + igniteDamageTaken;
            List<double> reports = new List<double>();
            reports.Add(fireballDamageTaken);
            reports.Add(igniteDamageTaken);
            reports.Add(totalDamageTaken);
            //reports.Add(nonCritCount);
            //reports.Add(resistCount);
            //reports.Add(critCount);
            //reports.Add(critCount + nonCritCount + resistCount);

            return reports;
        }
    }
    class Ignite
    {
        public Ignite()
        {
            stacks = 0;
            tickDamage = 0;
            igniteStatus = false;
            pulseCounter = 0;
            tickOfLastCrit = 0;
            tickStarted = 0;
            tickOfLastPulse = 0;
        }
        public int pulseCounter;
        public bool igniteStatus;
        private long tickStarted;
        private long tickOfLastCrit;
        private long tickOfLastPulse;
        private double tickDamage;
        private int stacks;
        private double igniteDamage;

        public void newCrit(long tick, double damage)
        {
            tickOfLastCrit = tick;
            pulseCounter = 0;
            
            if(stacks == 0)
            {
                igniteStatus = true;
                tickStarted = tick;
                tickOfLastPulse = tickStarted;
                tickDamage = 0;
                stacks++;
                AddPulseDamage(damage);
                //Console.WriteLine("Stacks now at " + stacks);
                
            }
            else if (stacks < 5)
            {
                stacks++;
                AddPulseDamage(damage);
                //Console.WriteLine("Stacks now at " + stacks);
            }

            return;
        }

        public void Pulse()
        {
            tickOfLastPulse += 200;
            //Console.WriteLine("Ignite pulsed at " + tickOfLastPulse + " for " + tickDamage);
            igniteDamage += Math.Round(tickDamage, 1);
            ////Console.WriteLine("Total Ignite damage at " + igniteDamage);
            pulseCounter++;
        }
        public void CheckStatus(long tick)
        {
            if(tick - tickOfLastCrit > 400 && igniteStatus == true)
            {
                if(pulseCounter == 1)
                {
                    Pulse();
                }
                else if(pulseCounter == 0)
                {
                    Pulse();
                    Pulse();
                }

                igniteStatus = false;
                stacks = 0;
                pulseCounter = 0;
            }
            else if(igniteStatus == true)
            {
                if(pulseCounter == 0 && (tick - tickOfLastPulse) >= 200)
                {
                    Pulse();
                }
                else if(pulseCounter == 1 && (tick - tickOfLastPulse) >= 200)
                {
                    Pulse();
                }
            }

        }
        public void AddPulseDamage(double damage)
        {
            // Spell modifiers on the target are:
            // Curse of Elements - 10%
            // Improved Scorch - 15%
            // 1.1 * 1.15 = 1.265
            tickDamage += Math.Round((damage * .2d * 1.265d), 1);// * 1.265d;
            //Console.WriteLine("Ignite tick damage now at " + tickDamage);
        }
        public double ReportIgnites()
        {
            return igniteDamage;
        }
    }
}
