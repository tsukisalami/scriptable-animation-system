using System;
using UnityEngine;
using Oyedoyin.Mathematics;


namespace Oyedoyin.Analysis
{
    [Serializable]
    public class Fuselage
    {
        [Header("Common")]
        public double ub;
        public double vb;
        public double wb;
        public double pb, qb, rb;
        public double ρ = 1.225;

        [Header("Output")]
        public Vector force;
        public Vector moment;

        /// <summary>
        /// 
        /// </summary>
        public virtual void Compute() { }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class CH47 : Fuselage
        {
            private double u, v, w;
            private readonly double Rb = 9.144;
            private AnimationCurve CFE_20;
            private AnimationCurve CFE0;
            private AnimationCurve CFE20;
            private readonly double CLα = 32.5;
            private readonly double CLβ = 6.57;
            private readonly double CMα = 142;
            private readonly double CNβ = 51.5;
            private readonly double CYβ = 43.4;
            private double CFE;
            private double Xfus, Yfus, Zfus;
            private double lfus, Mfus, Nfus;

            [Header("Forward Rotor")]
            public double λF;
            public double λpF;
            public double Ω = 24;

            [Header("Rear Rotor")]
            public double λR;
            public double λpR;

            public override void Initialize()
            {
                // ----------------------------------- CFE -20 alpha
                CFE_20 = new AnimationCurve();
                CFE_20.AddKey(new Keyframe(-19.95153774f, 1.37931034f));
                CFE_20.AddKey(new Keyframe(-18.40975458f, 1.62835249f));
                CFE_20.AddKey(new Keyframe(-16.96256601f, 1.87739464f));
                CFE_20.AddKey(new Keyframe(-14.94563529f, 2.14559387f));
                CFE_20.AddKey(new Keyframe(-12.74495185f, 2.36590038f));
                CFE_20.AddKey(new Keyframe(-10.45511028f, 2.53831418f));
                CFE_20.AddKey(new Keyframe(-8.16418142f, 2.72030651f));
                CFE_20.AddKey(new Keyframe(-6.37123330f, 2.68199234f));
                CFE_20.AddKey(new Keyframe(-4.11509786f, 2.55747126f));
                CFE_20.AddKey(new Keyframe(-1.67086052f, 2.42337165f));
                CFE_20.AddKey(new Keyframe(1.06694626f, 2.37547893f));
                CFE_20.AddKey(new Keyframe(4.00590245f, 2.43295019f));
                CFE_20.AddKey(new Keyframe(6.38164026f, 2.52873563f));
                CFE_20.AddKey(new Keyframe(8.94004349f, 2.56704981f));
                CFE_20.AddKey(new Keyframe(11.10593352f, 2.48084291f));
                CFE_20.AddKey(new Keyframe(13.26747437f, 2.35632184f));
                CFE_20.AddKey(new Keyframe(15.13762038f, 2.16475096f));
                CFE_20.AddKey(new Keyframe(17.00559180f, 1.95402299f));
                CFE_20.AddKey(new Keyframe(18.59195402f, 1.76245211f));
                CFE_20.AddKey(new Keyframe(19.98369059f, 1.52298851f));
                MathBase.LinearizeCurve(CFE_20);

                // ----------------------------------- CFE 0 alpha
                CFE0 = new AnimationCurve();
                CFE0.AddKey(new Keyframe(-20.08853681f, 3.30574713f));
                CFE0.AddKey(new Keyframe(-18.17598633f, 3.68773946f));
                CFE0.AddKey(new Keyframe(-15.88505747f, 3.86973180f));
                CFE0.AddKey(new Keyframe(-13.68763591f, 4.06130268f));
                CFE0.AddKey(new Keyframe(-11.87294191f, 4.21455939f));
                CFE0.AddKey(new Keyframe(-9.21233302f, 4.31992337f));
                CFE0.AddKey(new Keyframe(-6.56585896f, 4.30076628f));
                CFE0.AddKey(new Keyframe(-3.93460702f, 4.14750958f));
                CFE0.AddKey(new Keyframe(-1.86983535f, 4.00383142f));
                CFE0.AddKey(new Keyframe(0.96147872f, 3.94636015f));
                CFE0.AddKey(new Keyframe(4.00155328f, 4.06130268f));
                CFE0.AddKey(new Keyframe(7.04488972f, 4.20498084f));
                CFE0.AddKey(new Keyframe(10.26328052f, 4.22413793f));
                CFE0.AddKey(new Keyframe(13.56104380f, 4.10919540f));
                CFE0.AddKey(new Keyframe(15.81174278f, 3.93678161f));
                CFE0.AddKey(new Keyframe(17.86781609f, 3.71647510f));
                CFE0.AddKey(new Keyframe(19.92062752f, 3.46743295f));
                MathBase.LinearizeCurve(CFE0);


                // ----------------------------------- CFE 20 alpha
                CFE20 = new AnimationCurve();
                CFE20.AddKey(new Keyframe(-20.07875116f, 2.75862069f));
                CFE20.AddKey(new Keyframe(-18.44454800f, 2.98850575f));
                CFE20.AddKey(new Keyframe(-16.52221187f, 3.25670498f));
                CFE20.AddKey(new Keyframe(-14.60857409f, 3.44827586f));
                CFE20.AddKey(new Keyframe(-12.03603604f, 3.61111111f));
                CFE20.AddKey(new Keyframe(-9.74945635f, 3.75478927f));
                CFE20.AddKey(new Keyframe(-6.33317801f, 3.85057471f));
                CFE20.AddKey(new Keyframe(-3.21590556f, 3.81226054f));
                CFE20.AddKey(new Keyframe(-0.95433364f, 3.73563218f));
                CFE20.AddKey(new Keyframe(1.69322771f, 3.72605364f));
                CFE20.AddKey(new Keyframe(3.97110904f, 3.79310345f));
                CFE20.AddKey(new Keyframe(6.81329605f, 3.83141762f));
                CFE20.AddKey(new Keyframe(9.54892824f, 3.76436782f));
                CFE20.AddKey(new Keyframe(11.61804908f, 3.65900383f));
                CFE20.AddKey(new Keyframe(13.86983535f, 3.49616858f));
                CFE20.AddKey(new Keyframe(15.93678161f, 3.37164751f));
                CFE20.AddKey(new Keyframe(17.24262193f, 3.20881226f));
                CFE20.AddKey(new Keyframe(18.53867661f, 2.95977011f));
                CFE20.AddKey(new Keyframe(19.91736564f, 2.60536398f));
                MathBase.LinearizeCurve(CFE20);
            }
            /// <summary>
            /// 
            /// </summary>
            public override void Compute()
            {
                u = ub;
                v = vb;
                w = wb;
                double Wfus = w + (((λF - λpF) + (λR - λpR)) * (Ω * Rb));
                double Dfus1 = Math.Sqrt((u * u) + (Wfus * Wfus));
                double Dfus2 = Math.Sqrt((u * u) + (v * v));

                double sinαfus = 0;
                if (Dfus1 > 0) { sinαfus = Wfus / Dfus1; }
                double cosαfus = 1;
                if (Dfus1 > 0) { cosαfus = u / Dfus1; }
                double αfus = 0;
                if (Dfus1 > 0) { αfus = Math.Atan(Wfus / u); }
                double sinβfus = 0;
                if (Dfus2 > 0) { sinβfus = v / Dfus2; }
                double cosβfus = 1;
                if (Dfus2 > 0) { cosβfus = u / Dfus2; }
                double βfus = 0;
                if (Dfus2 > 0) { βfus = Math.Atan(v / u); }
                double αfusdeg = αfus * Mathf.Rad2Deg;
                double βfusdeg = βfus * Mathf.Rad2Deg;

                double m_cfen20 = CFE_20.Evaluate((float)βfusdeg);
                double m_cfe0 = CFE0.Evaluate((float)βfusdeg);
                double m_cfe20 = CFE20.Evaluate((float)βfusdeg);

                if (αfusdeg < -20) { CFE = m_cfen20; }
                if (αfusdeg >= -20 && αfusdeg < 0) { CFE = MathBase.Interpolate(αfusdeg, m_cfe0, m_cfen20, 0, -20); }
                if (αfusdeg >= 0 && αfusdeg <= 20) { CFE = MathBase.Interpolate(αfusdeg, m_cfe20, m_cfe0, 20, 0); }
                if (αfusdeg > 20) { CFE = m_cfe20; }

                double QDPRES = 0.5 * ρ * ((u * u) + (v * v) + (Wfus * Wfus));
                if (u >= 0) { Xfus = -CFE * QDPRES; }
                if (u < 0) { Xfus = CFE * QDPRES; }

                Yfus = -CYβ * QDPRES * sinβfus;
                Zfus = -CLα * QDPRES * sinαfus;
                lfus = -CLβ * sinβfus * Math.Abs(cosβfus) * (1 - Math.Abs(sinαfus));
                Mfus = CMα * QDPRES * sinαfus * cosαfus;
                Nfus = -CNβ * QDPRES * sinβfus * cosβfus * ((0.94 * sinαfus) + (0.342 * cosαfus));

                force = new Vector(Xfus, Yfus, Zfus);
                moment = new Vector(lfus, Mfus, Nfus);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class UH60 : Fuselage
        {
            [Header("Data")]
            public double ѡɪF;

            private AnimationCurve Dq;
            private AnimationCurve Lq;
            private AnimationCurve Yq;
            private AnimationCurve ΔDq;
            private AnimationCurve ΔLq;

            private AnimationCurve Mq;
            private AnimationCurve ΔMq;
            private AnimationCurve lq;
            private AnimationCurve Nq;
            private readonly double STAf = 8.7752;
            private readonly double WLf = 5.9436;
            private readonly double STAcg = 9.154;
            private readonly double WLcg = 6.279;
            private readonly double BLcg = 0;

            public override void Initialize()
            {
                Dq = new AnimationCurve();
                Dq.AddKey(new Keyframe(-90.2562089964221f, 150.12892211691295385f));
                Dq.AddKey(new Keyframe(-77.28210461594814f, 143.06992929233468904f));
                Dq.AddKey(new Keyframe(-68.82387184971515f, 131.0984216688579801f));
                Dq.AddKey(new Keyframe(-61.19555477064159f, 117.07462569415829962f));
                Dq.AddKey(new Keyframe(-55.61533983503723f, 101.41611559166161999f));
                Dq.AddKey(new Keyframe(-49.63743516283692f, 84.52162764990221055f));
                Dq.AddKey(new Keyframe(-43.650111523190915f, 68.8610244435287229f));
                Dq.AddKey(new Keyframe(-38.038500029434275f, 57.3154634589849998f));
                Dq.AddKey(new Keyframe(-28.77232916674842f, 44.517179804163991186f));
                Dq.AddKey(new Keyframe(-19.105328911651384f, 30.8942132218755653f));
                Dq.AddKey(new Keyframe(-8.5801560670578f, 23.0251891969676308f));
                Dq.AddKey(new Keyframe(8.5560853724744f, 27.87281777568469558f));
                Dq.AddKey(new Keyframe(22.892800376758714f, 39.31581667026421f));
                Dq.AddKey(new Keyframe(30.318086379780624f, 52.02828306613554036f));
                Dq.AddKey(new Keyframe(39.4032037571215f, 68.845326164452595f));
                Dq.AddKey(new Keyframe(48.0780727746054f, 85.253167454851123f));
                Dq.AddKey(new Keyframe(55.547313959040565f, 103.723762615856595f));
                Dq.AddKey(new Keyframe(63.41424488007169f, 120.95837993759935764f));
                Dq.AddKey(new Keyframe(69.6433220174905f, 136.9674849394635343f));
                Dq.AddKey(new Keyframe(80.72002773362635f, 148.0159337532623252f));
                Dq.AddKey(new Keyframe(87.26516355644515f, 152.09539320918615f));

                Lq = new AnimationCurve();
                Lq.AddKey(new Keyframe(-91.14529645512845f, -26.339064405712705872f));
                Lq.AddKey(new Keyframe(-86.7419443679427f, -37.01223590510288454056f));
                Lq.AddKey(new Keyframe(-82.34698561330342f, -48.5037573277727369653f));
                Lq.AddKey(new Keyframe(-77.12318526970137f, -59.183223826572765795f));
                Lq.AddKey(new Keyframe(-71.09572333477591f, -71.505685171342673f));
                Lq.AddKey(new Keyframe(-61.35526091461094f, -81.810599205256317943f));
                Lq.AddKey(new Keyframe(-51.12903437331967f, -84.753511429358262393f));
                Lq.AddKey(new Keyframe(-40.438027042268274f, -82.3802966518471636194f));
                Lq.AddKey(new Keyframe(-32.961666076510454f, -73.4351024904591356835f));
                Lq.AddKey(new Keyframe(-28.75031147132495f, -62.828028484872297076f));
                Lq.AddKey(new Keyframe(-24.05738941128638f, -45.268127631112992414f));
                Lq.AddKey(new Keyframe(-18.167368296809187f, -30.9910689695872631352f));
                Lq.AddKey(new Keyframe(-11.02988813261463f, -15.08675296061687765402f));
                Lq.AddKey(new Keyframe(-4.3278120942675855f, -1.634339221780663877184f));
                Lq.AddKey(new Keyframe(-0.19619414827343462f, 1.1984105126490476613f));
                Lq.AddKey(new Keyframe(6.112444426958348f, 16.2906715977495657f));
                Lq.AddKey(new Keyframe(16.91256508111374f, 29.30243537789672811f));
                Lq.AddKey(new Keyframe(34.68229924853446f, 41.85151670142036626f));
                Lq.AddKey(new Keyframe(51.9914492924683f, 49.493645984970743f));
                Lq.AddKey(new Keyframe(65.53304218961065f, 49.7989534563481661f));
                Lq.AddKey(new Keyframe(77.7558327103907f, 41.52102923240356f));
                Lq.AddKey(new Keyframe(87.82678259957251f, 23.4386434276272278f));


                Yq = new AnimationCurve();
                Yq.AddKey(new Keyframe(-89.38739877780787f, -37.14560944038047f));
                Yq.AddKey(new Keyframe(-83.85321653270718f, -52.15147957494207f));
                Yq.AddKey(new Keyframe(-78.7573376682019f, -67.15253318883771f));
                Yq.AddKey(new Keyframe(-71.46994190071948f, -82.17766940606279f));
                Yq.AddKey(new Keyframe(-61.1144224690689f, -97.23652126794906f));
                Yq.AddKey(new Keyframe(-50.65293958276888f, -102.62620789307323f));
                Yq.AddKey(new Keyframe(-41.34260513561517f, -93.05818959029472f));
                Yq.AddKey(new Keyframe(-31.95520635780727f, -76.45805111532556f));
                Yq.AddKey(new Keyframe(-23.449230861856137f, -60.287787109786564f));
                Yq.AddKey(new Keyframe(-18.459315452000396f, -44.958005960444325f));
                Yq.AddKey(new Keyframe(-11.253800535837911f, -27.454769860621923f));
                Yq.AddKey(new Keyframe(-4.953791504861698f, -12.578945784039263f));
                Yq.AddKey(new Keyframe(0.01204130166470918f, 0.5532978114934224f));
                Yq.AddKey(new Keyframe(8.108612541015674f, 19.365423402269798f));
                Yq.AddKey(new Keyframe(17.115506186218767f, 41.244468527047786f));
                Yq.AddKey(new Keyframe(26.584785815346635f, 65.31623468496946f));
                Yq.AddKey(new Keyframe(30.197176314759645f, 74.94686775640449f));
                Yq.AddKey(new Keyframe(39.12218910864266f, 89.35428519822995f));
                Yq.AddKey(new Keyframe(49.795598904241615f, 103.30292904662996f));
                Yq.AddKey(new Keyframe(59.842861013275524f, 100.11559649598124f));
                Yq.AddKey(new Keyframe(67.1543393840874f, 87.28799783256571f));
                Yq.AddKey(new Keyframe(74.8848550528312f, 72.69755260543666f));
                Yq.AddKey(new Keyframe(82.59128811824559f, 55.909569824498035f));
                Yq.AddKey(new Keyframe(88.12065384268038f, 40.464192179174574f));


                ΔDq = new AnimationCurve();
                ΔDq.AddKey(new Keyframe(-90.07482629609834f, 170.78233564938535f));
                ΔDq.AddKey(new Keyframe(-76.52592196686263f, 167.48610368786743f));
                ΔDq.AddKey(new Keyframe(-66.59005879208978f, 155.46886691608765f));
                ΔDq.AddKey(new Keyframe(-57.64831640833776f, 133.80946018172097f));
                ΔDq.AddKey(new Keyframe(-51.28808123997861f, 116.14377338321752f));
                ΔDq.AddKey(new Keyframe(-47.11918760021378f, 98.51416354890432f));
                ΔDq.AddKey(new Keyframe(-40.36878674505611f, 76.89083377872791f));
                ΔDq.AddKey(new Keyframe(-37.487974345269905f, 61.477552111170496f));
                ΔDq.AddKey(new Keyframe(-31.127739176910737f, 43.81186531266701f));
                ΔDq.AddKey(new Keyframe(-25.200427578834834f, 26.59233030464989f));
                ΔDq.AddKey(new Keyframe(-16.178514163548883f, 11.51696953500803f));
                ΔDq.AddKey(new Keyframe(-7.536076964190244f, 1.2771245323356482f));
                ΔDq.AddKey(new Keyframe(-0.5291288081239998f, 0.7227418492784352f));
                ΔDq.AddKey(new Keyframe(9.15553180117584f, 4.075494388027778f));
                ΔDq.AddKey(new Keyframe(18.476750400855167f, 13.580571886691615f));
                ΔDq.AddKey(new Keyframe(25.2485301977552f, 29.712987707108482f));
                ΔDq.AddKey(new Keyframe(31.603420630678812f, 47.608364510956704f));
                ΔDq.AddKey(new Keyframe(37.103153393907036f, 67.27391769107427f));
                ΔDq.AddKey(new Keyframe(42.17530732228758f, 87.82455905932655f));
                ΔDq.AddKey(new Keyframe(47.685729556387f, 108.36798503474076f));
                ΔDq.AddKey(new Keyframe(55.42490646712989f, 131.94788882950292f));
                ΔDq.AddKey(new Keyframe(62.21806520577232f, 149.8360502405131f));
                ΔDq.AddKey(new Keyframe(74.25975414216998f, 166.75975414216995f));
                ΔDq.AddKey(new Keyframe(84.81560662747194f, 169.65913949759485f));


                ΔLq = new AnimationCurve();
                ΔLq.AddKey(new Keyframe(-29.644152555104842f, 29.855557701800706f));
                ΔLq.AddKey(new Keyframe(-27.748159594788916f, 26.031163479492633f));
                ΔLq.AddKey(new Keyframe(-26.544545317966218f, 23.43356298156376f));
                ΔLq.AddKey(new Keyframe(-25.173953168930957f, 20.186723327538477f));
                ΔLq.AddKey(new Keyframe(-22.742257420642584f, 16.36168523168717f));
                ΔLq.AddKey(new Keyframe(-21.185371193097676f, 13.547313974202137f));
                ΔLq.AddKey(new Keyframe(-18.911639087416564f, 10.876311892344344f));
                ΔLq.AddKey(new Keyframe(-15.928787586118084f, 7.915995965059135f));
                ΔLq.AddKey(new Keyframe(-12.936921855214301f, 5.460476895671029f));
                ΔLq.AddKey(new Keyframe(-9.947631618483463f, 2.8607301525980375f));
                ΔLq.AddKey(new Keyframe(-5.856029875732403f, 1.9904277466572253f));
                ΔLq.AddKey(new Keyframe(-3.374111991071622f, 0.977829287661244f));
                ΔLq.AddKey(new Keyframe(-0.5311956731697762f, 0.18114309016375785f));
                ΔLq.AddKey(new Keyframe(4.3171721073981075f, 1.6897388019659658f));
                ΔLq.AddKey(new Keyframe(7.744725602558326f, 3.632734530938123f));
                ΔLq.AddKey(new Keyframe(11.358573176228191f, 6.0081986564505385f));
                ΔLq.AddKey(new Keyframe(13.908741656472003f, 8.817633550104095f));
                ΔLq.AddKey(new Keyframe(16.62975125018781f, 11.194170798188573f));
                ΔLq.AddKey(new Keyframe(19.5434936578456f, 14.363745627025523f));
                ΔLq.AddKey(new Keyframe(21.744253428626635f, 17.606292790762563f));
                ΔLq.AddKey(new Keyframe(24.48586697572597f, 21.13665142832615f));
                ΔLq.AddKey(new Keyframe(26.33077930161184f, 24.451741677934457f));
                ΔLq.AddKey(new Keyframe(28.884811023115077f, 27.477518082115346f));
                ΔLq.AddKey(new Keyframe(30.185006331423196f, 30.288455347369784f));


                Mq = new AnimationCurve();
                Mq.AddKey(new Keyframe(-90.43139142994704f, -202.64805007221958f));
                Mq.AddKey(new Keyframe(-86.47376023110255f, -294.36687530091467f));
                Mq.AddKey(new Keyframe(-82.07799711121811f, -403.5387578237842f));
                Mq.AddKey(new Keyframe(-76.79922965816081f, -517.116032739528f));
                Mq.AddKey(new Keyframe(-67.54935002407318f, -661.4106884930188f));
                Mq.AddKey(new Keyframe(-55.633124699085215f, -740.4910929224843f));
                Mq.AddKey(new Keyframe(-39.284545016851226f, -763.1680308136738f));
                Mq.AddKey(new Keyframe(-26.902262879152616f, -733.3413577274912f));
                Mq.AddKey(new Keyframe(-21.13914299470389f, -655.2238805970146f));
                Mq.AddKey(new Keyframe(-14.478574867597501f, -516.1531054405391f));
                Mq.AddKey(new Keyframe(-9.139142994703917f, -355.22388059701507f));
                Mq.AddKey(new Keyframe(-2.0211844005777664f, -146.46124217621582f));
                Mq.AddKey(new Keyframe(6.857968223399126f, 31.70438131921037f));
                Mq.AddKey(new Keyframe(12.207029369282594f, 236.20606644198347f));
                Mq.AddKey(new Keyframe(19.316321617717847f, 405.7534906114588f));
                Mq.AddKey(new Keyframe(25.544535387578208f, 588.4207992296581f));
                Mq.AddKey(new Keyframe(30.000962927298986f, 753.7554164660568f));
                Mq.AddKey(new Keyframe(43.27395281656234f, 814.0346653827635f));
                Mq.AddKey(new Keyframe(56.529610014443904f, 795.8834857968221f));
                Mq.AddKey(new Keyframe(64.46605681271066f, 708.3052479537794f));
                Mq.AddKey(new Keyframe(73.72075108329324f, 585.7968223399131f));
                Mq.AddKey(new Keyframe(79.87578237843044f, 437.3134328358208f));
                Mq.AddKey(new Keyframe(86.90707751564756f, 253.92392874337975f));


                ΔMq = new AnimationCurve();
                ΔMq.AddKey(new Keyframe(-29.999999999999993f, 180f));
                ΔMq.AddKey(new Keyframe(-28.57019990096171f, 166.4906565195861f));
                ΔMq.AddKey(new Keyframe(-26.724439000234558f, 150.36357476087468f));
                ΔMq.AddKey(new Keyframe(-25.10229612447548f, 132.9370064374886f));
                ΔMq.AddKey(new Keyframe(-23.264354035810157f, 115.50574682686543f));
                ΔMq.AddKey(new Keyframe(-20.997419792019592f, 97.63037869113086f));
                ΔMq.AddKey(new Keyframe(-19.15687143266699f, 80.63384503114494f));
                ΔMq.AddKey(new Keyframe(-17.50345330866063f, 68.42398811540565f));
                ΔMq.AddKey(new Keyframe(-15.447105736401781f, 51.422763168182655f));
                ΔMq.AddKey(new Keyframe(-12.735541713362345f, 35.71164221116004f));
                ΔMq.AddKey(new Keyframe(-10.021371419635635f, 20.43524720477467f));
                ΔMq.AddKey(new Keyframe(-4.928197242565609f, 5.9766999400557665f));
                ΔMq.AddKey(new Keyframe(0.21710234824989527f, 0.21267168808154224f));
                ΔMq.AddKey(new Keyframe(5.456227683807249f, 10.09877765904767f));
                ΔMq.AddKey(new Keyframe(9.826943626365058f, 19.134196877687714f));
                ΔMq.AddKey(new Keyframe(12.760040657822735f, 40.374781724829944f));
                ΔMq.AddKey(new Keyframe(15.253720451406089f, 60.32057129453466f));
                ΔMq.AddKey(new Keyframe(18.389585342333667f, 79.38283510125362f));
                ΔMq.AddKey(new Keyframe(21.327894915165892f, 101.4928718496703f));
                ΔMq.AddKey(new Keyframe(23.8372123328729f, 124.04701712319839f));
                ΔMq.AddKey(new Keyframe(25.907112512705588f, 145.306367119289f));
                ΔMq.AddKey(new Keyframe(28.182386822695413f, 164.82212202559356f));
                ΔMq.AddKey(new Keyframe(30.002606270687288f, 180.43472595063722f));


                lq = new AnimationCurve();
                lq.AddKey(new Keyframe(-89.01582608111285f, 99.9966398978529f));
                lq.AddKey(new Keyframe(-79.77420113571452f, 99.39719767480932f));
                lq.AddKey(new Keyframe(-67.27999731191827f, 100.14582843318438f));
                lq.AddKey(new Keyframe(-52.457914720607505f, 102.25597258156648f));
                lq.AddKey(new Keyframe(-38.0766775309969f, 106.23298948288028f));
                lq.AddKey(new Keyframe(-28.297436242061735f, 112.13736097577367f));
                lq.AddKey(new Keyframe(-24.985719565874774f, 118.60085346594536f));
                lq.AddKey(new Keyframe(-22.86751117233962f, 101.82587950673701f));
                lq.AddKey(new Keyframe(-20.711669634756873f, 88.30617250764423f));
                lq.AddKey(new Keyframe(-19.502032861798966f, 72.93975336850241f));
                lq.AddKey(new Keyframe(-17.8515506871409f, 55.706461476428885f));
                lq.AddKey(new Keyframe(-16.17956385874129f, 40.33332213299285f));
                lq.AddKey(new Keyframe(-13.13127919088737f, 24.009945902355454f));
                lq.AddKey(new Keyframe(-11.443163872181685f, 10.031920970397522f));
                lq.AddKey(new Keyframe(-10.64211552031179f, -0.677396592856411f));
                lq.AddKey(new Keyframe(-0.008064245153008187f, -0.8319612916232586f));
                lq.AddKey(new Keyframe(9.238936863680692f, -0.9663653775074579f));
                lq.AddKey(new Keyframe(10.927052182386376f, -14.944390309465405f));
                lq.AddKey(new Keyframe(14.883908470817573f, -32.67632136016931f));
                lq.AddKey(new Keyframe(15.152716642585972f, -49.42441450220085f));
                lq.AddKey(new Keyframe(18.195625147004506f, -66.21282886999762f));
                lq.AddKey(new Keyframe(19.389133429656255f, -82.97436242061758f));
                lq.AddKey(new Keyframe(21.969691878633142f, -99.75605658412013f));
                lq.AddKey(new Keyframe(24.050267128120737f, -119.78629750344408f));
                lq.AddKey(new Keyframe(30.630691173011712f, -110.57961762037564f));
                lq.AddKey(new Keyframe(39.92070158932836f, -106.99371660898493f));
                lq.AddKey(new Keyframe(53.83958872349723f, -103.00997950337688f));
                lq.AddKey(new Keyframe(67.73697120392467f, -100.88639494640634f));
                lq.AddKey(new Keyframe(87.16642585934619f, -100.23856725244445f));


                Nq = new AnimationCurve();
                Nq.AddKey(new Keyframe(-89.3460721868365f, 446.89490445859866f));
                Nq.AddKey(new Keyframe(-84.35987261146494f, 421.0589171974522f));
                Nq.AddKey(new Keyframe(-76.21974522292993f, 371.8550955414012f));
                Nq.AddKey(new Keyframe(-67.17940552016984f, 315.64490445859866f));
                Nq.AddKey(new Keyframe(-59.05201698513798f, 261.8232484076433f));
                Nq.AddKey(new Keyframe(-52.795116772823775f, 196.61624203821657f));
                Nq.AddKey(new Keyframe(-47.90445859872611f, 136.14649681528658f));
                Nq.AddKey(new Keyframe(-40.74097664543521f, 66.24203821656045f));
                Nq.AddKey(new Keyframe(-35.89490445859872f, -10.39012738853512f));
                Nq.AddKey(new Keyframe(-31.112526539278093f, -110.11146496815286f));
                Nq.AddKey(new Keyframe(-26.24097664543521f, -177.50796178343955f));
                Nq.AddKey(new Keyframe(-20.897027600849214f, -240.32643312101925f));
                Nq.AddKey(new Keyframe(-12.061571125265345f, -204.140127388535f));
                Nq.AddKey(new Keyframe(-7.261146496815286f, -130.65286624203827f));
                Nq.AddKey(new Keyframe(-3.3545647558386236f, -47.85031847133769f));
                Nq.AddKey(new Keyframe(-0.9097664543523933f, 5.0557324840764295f));
                Nq.AddKey(new Keyframe(4.420382165605105f, 103.90127388535029f));
                Nq.AddKey(new Keyframe(10.587048832271819f, 172.6512738853503f));
                Nq.AddKey(new Keyframe(16.715498938428937f, 227.54777070063687f));
                Nq.AddKey(new Keyframe(25.774946921443757f, 178.26433121019102f));
                Nq.AddKey(new Keyframe(35.63906581740983f, 87.34076433121015f));
                Nq.AddKey(new Keyframe(45.08174097664550f, 10.31050955414014f));
                Nq.AddKey(new Keyframe(52.68577494692147f, -66.56050955414014f));
                Nq.AddKey(new Keyframe(61.662420382165635f, -145.859872611465f));
                Nq.AddKey(new Keyframe(70.6390658174098f, -225.15923566878985f));
                Nq.AddKey(new Keyframe(78.65817409766458f, -318.23248407643314f));
                Nq.AddKey(new Keyframe(87.62845010615717f, -399.84076433121027f));

                MathBase.LinearizeCurve(Nq);
                MathBase.LinearizeCurve(lq);
                MathBase.LinearizeCurve(ΔMq);
                MathBase.LinearizeCurve(Mq);
                MathBase.LinearizeCurve(ΔLq);
                MathBase.LinearizeCurve(ΔDq);
                MathBase.LinearizeCurve(Yq);
                MathBase.LinearizeCurve(Dq);
                MathBase.LinearizeCurve(Lq);
            }
            public override void Compute()
            {
                double uF = ub;
                double vF = vb - (rb * (STAf - STAcg));
                double wF = wb + (qb * (STAf - STAcg)) - ѡɪF;

                double αF = Math.Atan(wF / Math.Abs(uF)) * Mathf.Rad2Deg;
                double vx = Math.Sqrt((uF * uF) + (wF * wF));
                double βF = Math.Atan(vF / vx);
                double ψw = -βF * Mathf.Rad2Deg;
                if (double.IsNaN(αF) || double.IsInfinity(αF)) { αF = 0.0; }
                if (double.IsNaN(βF) || double.IsInfinity(βF)) { βF = 0.0; }
                if (double.IsNaN(ψw) || double.IsInfinity(ψw)) { ψw = 0.0; }

                double m_Dq = Dq.Evaluate((float)αF) * 0.0929;
                double m_Lq = Lq.Evaluate((float)αF) * 0.0929;
                double m_Mq = Mq.Evaluate((float)αF) * 0.0929;
                double m_ΔDq = ΔDq.Evaluate((float)ψw) * 0.0929;
                double m_ΔLq = ΔLq.Evaluate((float)ψw) * 0.0929;
                double m_ΔMq = ΔMq.Evaluate((float)ψw) * 0.0929;
                double m_Yq = Yq.Evaluate((float)ψw) * 0.0929;
                double m_lq = lq.Evaluate((float)ψw) * 0.0929;
                double m_Nq = Nq.Evaluate((float)ψw) * 0.0929;

                double Vm = Math.Sqrt((ub * ub) + (vb * vb) + (wb * wb));
                double Q = 0.5 * Vm * Vm * ρ;
                double m_D = (m_Dq * m_ΔDq) * Q;
                double m_Y = m_Yq * Q;
                double m_L = (m_Lq + m_ΔLq) * Q;
                double m_l = m_lq * Q;
                double m_M = (m_Mq + m_ΔMq) * Q;
                double m_N = m_Nq * Q;

                double cosαf = Math.Cos(αF * Mathf.Deg2Rad);
                double sinαf = Math.Sin(αF * Mathf.Deg2Rad);
                double cosβf = Math.Cos(βF);
                double sinβf = Math.Sin(βF);

                double Xf = (-m_D * cosβf * cosαf) - (m_Y * sinβf * sinαf) + (m_L * sinαf);
                double Yf = (m_Y * cosβf) - (m_D * sinβf);
                double Zf = (m_L * cosαf) - (m_D * cosβf * sinαf) - (m_Y * sinαf * sinβf);
                force = new Vector(Xf, Yf, Zf);

                double lf = (m_l * cosαf * cosβf) - (m_M * sinβf * cosαf) - (m_N * sinαf) + (Yf * WLf - WLcg) - (Zf * BLcg);
                double Mf = (m_M * cosβf) + (m_l * sinβf) - (Xf * (WLf - WLcg)) + (Zf * (STAf - STAcg));
                double Nf = (m_N * cosαf) + (m_l * cosβf * cosαf) - (m_M * sinβf * sinαf) + (Yf * (STAcg - STAf)) + (Xf * BLcg);
                moment = new Vector(lf, Mf, Nf);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class CH54 : Fuselage
        {
            private readonly double ekf = 0.5f;
            private readonly double ekt = 1.8f;
            private readonly double kfe = 0.0243f;
            private Vector m_fuse = new Vector(-0.51, 0, -0.37);

            private AnimationCurve ΔL1hq;
            private AnimationCurve ΔL2hq;
            private AnimationCurve Yhq;
            private AnimationCurve Lwthq;
            private AnimationCurve ΔM1wt_N10;
            private AnimationCurve ΔM1wt_0;
            private AnimationCurve ΔM1wt_P10;
            private AnimationCurve ΔM2wt;
            private AnimationCurve Nwt_P10;
            private AnimationCurve Nwt_0;
            private AnimationCurve Nwt_N10;
            private AnimationCurve Nwt_N20;

            [Header("Main Rotor Data")]
            public double CT;
            public double λ, μ;
            public double Thrust;

            /// <summary>
            /// 
            /// </summary>
            public override void Initialize()
            {
                ΔL1hq = new AnimationCurve();
                ΔL1hq.AddKey(new Keyframe(-28.028f, -7.262f));
                ΔL1hq.AddKey(new Keyframe(-24.044f, -7.284f));
                ΔL1hq.AddKey(new Keyframe(-20.061f, -7.251f));
                ΔL1hq.AddKey(new Keyframe(-18.073f, -6.356f));
                ΔL1hq.AddKey(new Keyframe(-15.998f, -5.487f));
                ΔL1hq.AddKey(new Keyframe(-14.271f, -4.402f));
                ΔL1hq.AddKey(new Keyframe(-12.025f, -3.073f));
                ΔL1hq.AddKey(new Keyframe(-10.125f, -1.799f));
                ΔL1hq.AddKey(new Keyframe(-7.966f, -0.362f));
                ΔL1hq.AddKey(new Keyframe(-5.807f, 0.940f));
                ΔL1hq.AddKey(new Keyframe(-3.992f, 1.998f));
                ΔL1hq.AddKey(new Keyframe(-2.177f, 2.704f));
                ΔL1hq.AddKey(new Keyframe(-0.015f, 3.518f));
                ΔL1hq.AddKey(new Keyframe(4.138f, 4.417f));
                ΔL1hq.AddKey(new Keyframe(7.947f, 4.638f));
                ΔL1hq.AddKey(new Keyframe(10.195f, 5.534f));
                ΔL1hq.AddKey(new Keyframe(11.924f, 6.321f));
                ΔL1hq.AddKey(new Keyframe(16.073f, 8.112f));
                ΔL1hq.AddKey(new Keyframe(19.965f, 9.281f));
                ΔL1hq.AddKey(new Keyframe(23.949f, 9.313f));
                ΔL1hq.AddKey(new Keyframe(27.933f, 9.291f));
                MathBase.LinearizeCurve(ΔL1hq);



                ΔL2hq = new AnimationCurve();
                ΔL2hq.AddKey(new Keyframe(-35.173f, 2.605f));
                ΔL2hq.AddKey(new Keyframe(-30.200f, 2.605f));
                ΔL2hq.AddKey(new Keyframe(-29.239f, 2.141f));
                ΔL2hq.AddKey(new Keyframe(-28.602f, 1.719f));
                ΔL2hq.AddKey(new Keyframe(-27.857f, 1.297f));
                ΔL2hq.AddKey(new Keyframe(-26.683f, 0.735f));
                ΔL2hq.AddKey(new Keyframe(-25.831f, 0.249f));
                ΔL2hq.AddKey(new Keyframe(-25.194f, -0.173f));
                ΔL2hq.AddKey(new Keyframe(-23.256f, -0.465f));
                ΔL2hq.AddKey(new Keyframe(-19.917f, -0.930f));
                ΔL2hq.AddKey(new Keyframe(-15.055f, -1.049f));
                ΔL2hq.AddKey(new Keyframe(-10.179f, -0.616f));
                ΔL2hq.AddKey(new Keyframe(-7.680f, -0.173f));
                ΔL2hq.AddKey(new Keyframe(-5.074f, 0.270f));
                ΔL2hq.AddKey(new Keyframe(-0.108f, 0.011f));
                ΔL2hq.AddKey(new Keyframe(2.476f, -0.389f));
                ΔL2hq.AddKey(new Keyframe(4.953f, -0.735f));
                ΔL2hq.AddKey(new Keyframe(7.532f, -1.319f));
                ΔL2hq.AddKey(new Keyframe(10.003f, -1.881f));
                ΔL2hq.AddKey(new Keyframe(12.477f, -2.346f));
                ΔL2hq.AddKey(new Keyframe(14.951f, -2.822f));
                ΔL2hq.AddKey(new Keyframe(20.023f, -3.135f));
                ΔL2hq.AddKey(new Keyframe(21.006f, -2.778f));
                ΔL2hq.AddKey(new Keyframe(22.537f, -2.141f));
                ΔL2hq.AddKey(new Keyframe(23.847f, -1.654f));
                ΔL2hq.AddKey(new Keyframe(24.941f, -1.200f));
                ΔL2hq.AddKey(new Keyframe(26.907f, -0.454f));
                ΔL2hq.AddKey(new Keyframe(28.110f, 0.076f));
                ΔL2hq.AddKey(new Keyframe(29.968f, 0.800f));
                ΔL2hq.AddKey(new Keyframe(34.941f, 0.800f));
                MathBase.LinearizeCurve(ΔL2hq);


                Yhq = new AnimationCurve();
                Yhq.AddKey(new Keyframe(-35.176f, -10.993f));
                Yhq.AddKey(new Keyframe(-29.985f, -10.910f));
                Yhq.AddKey(new Keyframe(-27.484f, -9.788f));
                Yhq.AddKey(new Keyframe(-24.981f, -8.623f));
                Yhq.AddKey(new Keyframe(-20.003f, -8.238f));
                Yhq.AddKey(new Keyframe(-15.015f, -7.074f));
                Yhq.AddKey(new Keyframe(-9.905f, -4.961f));
                Yhq.AddKey(new Keyframe(-5.009f, -2.632f));
                Yhq.AddKey(new Keyframe(-0.108f, 0.043f));
                Yhq.AddKey(new Keyframe(4.911f, 3.409f));
                Yhq.AddKey(new Keyframe(10.032f, 6.343f));
                Yhq.AddKey(new Keyframe(15.039f, 8.888f));
                Yhq.AddKey(new Keyframe(20.036f, 10.656f));
                Yhq.AddKey(new Keyframe(25.025f, 11.819f));
                Yhq.AddKey(new Keyframe(29.898f, 12.421f));
                Yhq.AddKey(new Keyframe(34.979f, 12.418f));
                MathBase.LinearizeCurve(Yhq);


                Lwthq = new AnimationCurve();
                Lwthq.AddKey(new Keyframe(-35.068f, 19.459f));
                Lwthq.AddKey(new Keyframe(-29.987f, 19.459f));
                Lwthq.AddKey(new Keyframe(-25.124f, 19.265f));
                Lwthq.AddKey(new Keyframe(-19.839f, 17.838f));
                Lwthq.AddKey(new Keyframe(-14.995f, 15.568f));
                Lwthq.AddKey(new Keyframe(-9.838f, 12.000f));
                Lwthq.AddKey(new Keyframe(-7.587f, 9.795f));
                Lwthq.AddKey(new Keyframe(-4.909f, 7.070f));
                Lwthq.AddKey(new Keyframe(-2.775f, 3.957f));
                Lwthq.AddKey(new Keyframe(-0.000f, -0.000f));
                Lwthq.AddKey(new Keyframe(2.126f, -4.022f));
                Lwthq.AddKey(new Keyframe(4.997f, -9.341f));
                Lwthq.AddKey(new Keyframe(6.919f, -12.000f));
                Lwthq.AddKey(new Keyframe(10.017f, -16.086f));
                Lwthq.AddKey(new Keyframe(15.064f, -19.849f));
                Lwthq.AddKey(new Keyframe(19.951f, -17.449f));
                Lwthq.AddKey(new Keyframe(22.467f, -14.141f));
                Lwthq.AddKey(new Keyframe(24.983f, -10.832f));
                Lwthq.AddKey(new Keyframe(27.081f, -5.968f));
                Lwthq.AddKey(new Keyframe(28.626f, -2.530f));
                Lwthq.AddKey(new Keyframe(29.513f, -0.065f));
                Lwthq.AddKey(new Keyframe(29.954f, 0.908f));
                Lwthq.AddKey(new Keyframe(34.927f, 0.908f));
                MathBase.LinearizeCurve(Lwthq);


                ΔM1wt_P10 = new AnimationCurve();
                ΔM1wt_P10.AddKey(new Keyframe(-28.020f, 1.667f));
                ΔM1wt_P10.AddKey(new Keyframe(-23.966f, 1.664f));
                ΔM1wt_P10.AddKey(new Keyframe(-19.999f, 1.661f));
                ΔM1wt_P10.AddKey(new Keyframe(-16.126f, -0.191f));
                ΔM1wt_P10.AddKey(new Keyframe(-14.324f, -2.298f));
                ΔM1wt_P10.AddKey(new Keyframe(-12.007f, -5.021f));
                ΔM1wt_P10.AddKey(new Keyframe(-10.206f, -7.180f));
                ΔM1wt_P10.AddKey(new Keyframe(-8.061f, -9.749f));
                ΔM1wt_P10.AddKey(new Keyframe(-4.021f, -12.782f));
                ΔM1wt_P10.AddKey(new Keyframe(-1.857f, -10.986f));
                ΔM1wt_P10.AddKey(new Keyframe(0.048f, -9.499f));
                ΔM1wt_P10.AddKey(new Keyframe(4.032f, -5.907f));
                ΔM1wt_P10.AddKey(new Keyframe(7.924f, -3.600f));
                ΔM1wt_P10.AddKey(new Keyframe(11.896f, -2.473f));
                ΔM1wt_P10.AddKey(new Keyframe(15.955f, -1.449f));
                ΔM1wt_P10.AddKey(new Keyframe(19.829f, -2.942f));
                ΔM1wt_P10.AddKey(new Keyframe(23.969f, -2.996f));
                ΔM1wt_P10.AddKey(new Keyframe(28.023f, -3.000f));
                MathBase.LinearizeCurve(ΔM1wt_P10);

                ΔM1wt_N10 = new AnimationCurve();
                ΔM1wt_N10.AddKey(new Keyframe(-27.926f, 3.516f));
                ΔM1wt_N10.AddKey(new Keyframe(-24.044f, 3.461f));
                ΔM1wt_N10.AddKey(new Keyframe(-20.163f, 3.458f));
                ΔM1wt_N10.AddKey(new Keyframe(-16.110f, 3.198f));
                ΔM1wt_N10.AddKey(new Keyframe(-12.138f, 4.119f));
                ΔM1wt_N10.AddKey(new Keyframe(-7.983f, 7.505f));
                ΔM1wt_N10.AddKey(new Keyframe(-4.099f, 8.067f));
                ΔM1wt_N10.AddKey(new Keyframe(0.052f, 10.374f));
                ΔM1wt_N10.AddKey(new Keyframe(3.850f, 11.193f));
                ΔM1wt_N10.AddKey(new Keyframe(7.985f, 9.906f));
                ΔM1wt_N10.AddKey(new Keyframe(11.948f, 8.927f));
                ΔM1wt_N10.AddKey(new Keyframe(15.831f, 9.335f));
                ΔM1wt_N10.AddKey(new Keyframe(19.962f, 7.226f));
                ΔM1wt_N10.AddKey(new Keyframe(23.929f, 7.120f));
                ΔM1wt_N10.AddKey(new Keyframe(28.156f, 7.168f));
                MathBase.LinearizeCurve(ΔM1wt_N10);

                ΔM1wt_0 = new AnimationCurve();
                ΔM1wt_0.AddKey(new Keyframe(-27.996f, 7.008f));
                ΔM1wt_0.AddKey(new Keyframe(-24.115f, 6.850f));
                ΔM1wt_0.AddKey(new Keyframe(-20.233f, 6.950f));
                ΔM1wt_0.AddKey(new Keyframe(-16.086f, 8.641f));
                ΔM1wt_0.AddKey(new Keyframe(-12.046f, 5.506f));
                ΔM1wt_0.AddKey(new Keyframe(-8.008f, 2.062f));
                ΔM1wt_0.AddKey(new Keyframe(-3.960f, 0.672f));
                ΔM1wt_0.AddKey(new Keyframe(0.012f, 1.542f));
                ΔM1wt_0.AddKey(new Keyframe(4.070f, 2.617f));
                ΔM1wt_0.AddKey(new Keyframe(7.952f, 2.717f));
                ΔM1wt_0.AddKey(new Keyframe(11.836f, 3.176f));
                ΔM1wt_0.AddKey(new Keyframe(15.976f, 3.121f));
                ΔM1wt_0.AddKey(new Keyframe(19.938f, 1.988f));
                ΔM1wt_0.AddKey(new Keyframe(23.905f, 1.779f));
                ΔM1wt_0.AddKey(new Keyframe(27.873f, 1.828f));
                MathBase.LinearizeCurve(ΔM1wt_0);

                ΔM2wt = new AnimationCurve();
                ΔM2wt.AddKey(new Keyframe(-35.054f, -30.214f));
                ΔM2wt.AddKey(new Keyframe(-30.081f, -30.285f));
                ΔM2wt.AddKey(new Keyframe(-25.038f, -35.683f));
                ΔM2wt.AddKey(new Keyframe(-23.744f, -32.071f));
                ΔM2wt.AddKey(new Keyframe(-21.911f, -26.938f));
                ΔM2wt.AddKey(new Keyframe(-20.078f, -21.868f));
                ΔM2wt.AddKey(new Keyframe(-17.725f, -18.004f));
                ΔM2wt.AddKey(new Keyframe(-15.261f, -13.442f));
                ΔM2wt.AddKey(new Keyframe(-10.028f, -6.222f));
                ΔM2wt.AddKey(new Keyframe(-5.037f, -3.567f));
                ΔM2wt.AddKey(new Keyframe(-0.040f, -0.087f));
                ΔM2wt.AddKey(new Keyframe(4.838f, 1.490f));
                ΔM2wt.AddKey(new Keyframe(9.806f, 0.722f));
                ΔM2wt.AddKey(new Keyframe(14.875f, -0.872f));
                ΔM2wt.AddKey(new Keyframe(19.942f, -2.782f));
                ΔM2wt.AddKey(new Keyframe(24.902f, -4.819f));
                ΔM2wt.AddKey(new Keyframe(29.856f, -7.743f));
                ΔM2wt.AddKey(new Keyframe(34.829f, -7.815f));
                MathBase.LinearizeCurve(ΔM2wt);

                Nwt_P10 = new AnimationCurve();
                Nwt_P10.AddKey(new Keyframe(-35.037f, -13.654f));
                Nwt_P10.AddKey(new Keyframe(-30.182f, -13.575f));
                Nwt_P10.AddKey(new Keyframe(-24.997f, -5.580f));
                Nwt_P10.AddKey(new Keyframe(-20.151f, -10.646f));
                Nwt_P10.AddKey(new Keyframe(-14.979f, -10.726f));
                Nwt_P10.AddKey(new Keyframe(-10.011f, -6.372f));
                Nwt_P10.AddKey(new Keyframe(-5.146f, -0.673f));
                Nwt_P10.AddKey(new Keyframe(-0.074f, 2.968f));
                Nwt_P10.AddKey(new Keyframe(5.009f, 12.625f));
                Nwt_P10.AddKey(new Keyframe(9.879f, 21.174f));
                Nwt_P10.AddKey(new Keyframe(14.745f, 27.427f));
                Nwt_P10.AddKey(new Keyframe(19.714f, 32.573f));
                Nwt_P10.AddKey(new Keyframe(24.671f, 30.673f));
                Nwt_P10.AddKey(new Keyframe(29.834f, 25.844f));
                Nwt_P10.AddKey(new Keyframe(34.900f, 25.686f));
                MathBase.LinearizeCurve(Nwt_P10);


                Nwt_0 = new AnimationCurve();
                Nwt_0.AddKey(new Keyframe(-35.130f, -6.214f));
                Nwt_0.AddKey(new Keyframe(-30.169f, -6.214f));
                Nwt_0.AddKey(new Keyframe(-24.883f, -0.989f));
                Nwt_0.AddKey(new Keyframe(-19.824f, -4.789f));
                Nwt_0.AddKey(new Keyframe(-14.973f, -7.084f));
                Nwt_0.AddKey(new Keyframe(-9.902f, -4.235f));
                Nwt_0.AddKey(new Keyframe(-5.254f, -1.702f));
                Nwt_0.AddKey(new Keyframe(-0.607f, 0.040f));
                Nwt_0.AddKey(new Keyframe(4.895f, 7.797f));
                Nwt_0.AddKey(new Keyframe(9.868f, 14.921f));
                Nwt_0.AddKey(new Keyframe(14.833f, 17.533f));
                Nwt_0.AddKey(new Keyframe(19.800f, 21.174f));
                Nwt_0.AddKey(new Keyframe(24.862f, 19.037f));
                Nwt_0.AddKey(new Keyframe(30.024f, 13.575f));
                Nwt_0.AddKey(new Keyframe(34.984f, 13.575f));
                MathBase.LinearizeCurve(Nwt_0);

                Nwt_N10 = new AnimationCurve();
                Nwt_N10.AddKey(new Keyframe(-35.107f, 6.768f));
                Nwt_N10.AddKey(new Keyframe(-30.041f, 6.847f));
                Nwt_N10.AddKey(new Keyframe(-25.074f, 10.409f));
                Nwt_N10.AddKey(new Keyframe(-20.233f, 2.335f));
                Nwt_N10.AddKey(new Keyframe(-15.074f, -4.551f));
                Nwt_N10.AddKey(new Keyframe(-10.006f, -3.522f));
                Nwt_N10.AddKey(new Keyframe(-4.941f, -3.681f));
                Nwt_N10.AddKey(new Keyframe(-0.190f, -3.047f));
                Nwt_N10.AddKey(new Keyframe(4.884f, 1.544f));
                Nwt_N10.AddKey(new Keyframe(9.852f, 5.660f));
                Nwt_N10.AddKey(new Keyframe(14.921f, 7.718f));
                Nwt_N10.AddKey(new Keyframe(19.989f, 8.747f));
                Nwt_N10.AddKey(new Keyframe(25.047f, 4.314f));
                Nwt_N10.AddKey(new Keyframe(29.985f, -8.430f));
                Nwt_N10.AddKey(new Keyframe(35.157f, -8.509f));
                MathBase.LinearizeCurve(Nwt_N10);

                Nwt_N20 = new AnimationCurve();
                Nwt_N20.AddKey(new Keyframe(-35.108f, 6.214f));
                Nwt_N20.AddKey(new Keyframe(-30.147f, 6.293f));
                Nwt_N20.AddKey(new Keyframe(-24.968f, 10.409f));
                Nwt_N20.AddKey(new Keyframe(-20.121f, 6.214f));
                Nwt_N20.AddKey(new Keyframe(-14.752f, -1.464f));
                Nwt_N20.AddKey(new Keyframe(-9.895f, -0.040f));
                Nwt_N20.AddKey(new Keyframe(-5.145f, -0.198f));
                Nwt_N20.AddKey(new Keyframe(-0.082f, -1.702f));
                Nwt_N20.AddKey(new Keyframe(4.883f, 0.752f));
                Nwt_N20.AddKey(new Keyframe(9.952f, 2.493f));
                Nwt_N20.AddKey(new Keyframe(14.908f, 0.198f));
                Nwt_N20.AddKey(new Keyframe(19.867f, -0.831f));
                Nwt_N20.AddKey(new Keyframe(24.716f, -4.235f));
                Nwt_N20.AddKey(new Keyframe(29.882f, -7.084f));
                Nwt_N20.AddKey(new Keyframe(35.053f, -7.243f));
                MathBase.LinearizeCurve(Nwt_N20);
            }
            /// <summary>
            /// 
            /// </summary>
            public override void Compute()
            {
                double V = Math.Sqrt((ub * ub) + (vb * vb) + (wb * wb));
                double emr = CT / (2 * ((λ * λ) + (μ * μ)));
                double αf = Math.Atan(wb / ub);
                double αfdeg = αf * Mathf.Rad2Deg;
                double αfl = αf - (emr * ekf);
                double αfldeg = αfl * Mathf.Rad2Deg;
                double it = 0 - (emr * (ekt - ekf));
                double itdeg = it * Mathf.Rad2Deg;
                double βf = Math.Asin(vb / V);
                double ψwt = -βf;
                double ψwtdeg = ψwt * Mathf.Rad2Deg;
                if (double.IsNaN(ψwtdeg) || double.IsInfinity(ψwtdeg)) { ψwtdeg = 0.0; }
                if (double.IsNaN(αf) || double.IsInfinity(αf)) { αf = 0.0; }
                if (double.IsNaN(αfdeg) || double.IsInfinity(αfdeg)) { αfdeg = 0.0; }
                if (double.IsNaN(αfl) || double.IsInfinity(αfl)) { αfl = 0.0; }
                if (double.IsNaN(αfldeg) || double.IsInfinity(αfldeg)) { αfldeg = 0.0; }
                if (double.IsNaN(it) || double.IsInfinity(it)) { it = 0.0; }
                if (double.IsNaN(itdeg) || double.IsInfinity(itdeg)) { itdeg = 0.0; }
                if (double.IsNaN(βf) || double.IsInfinity(βf)) { βf = 0.0; }
                if (double.IsNaN(ψwt) || double.IsInfinity(ψwt)) { ψwt = 0.0; }

                double Q = 0.5 * V * V * ρ;
                double Dh = Q * (7.25f + (2.4f * αfl) + (42.9f * αfl * αfl) + (45.6f * ψwt * ψwt));
                double Yh = 0f;
                double ΔL1h_qh = ΔL1hq.Evaluate((float)αfldeg);
                double ΔL2h_qh = ΔL2hq.Evaluate((float)ψwtdeg);
                double Lh = (ΔL1h_qh + ΔL2h_qh) * Q;
                double cosαf = Math.Cos(αfl);
                double sinαf = Math.Sin(αfl);
                double cosβf = Math.Cos(βf);
                double sinβf = Math.Sin(βf);
                Vector fhr1 = new Vector((-cosαf * cosβf), (-cosαf * sinβf), sinαf);
                Vector fhr2 = new Vector(-sinβf, cosβf, 0);
                Vector fhr3 = new Vector((-sinαf * cosβf), (-sinαf * sinβf), -cosαf);
                Matrix3x3 Sf = new Matrix3x3(fhr1, fhr2, fhr3);
                Vector Fh = new Vector(Dh, Yh, Lh);
                force = Sf * Fh;

                double ΔM1wt_qh = 0f;
                double ideg = itdeg;
                if (ideg < -10) { ideg = -10f; }
                if (ideg > 10) { ideg = 10f; }
                double m1n10 = ΔM1wt_N10.Evaluate((float)αfl);
                double m10 = ΔM1wt_0.Evaluate((float)αfl);
                double m1p10 = ΔM1wt_P10.Evaluate((float)αfl);
                if (ideg >= -10 && ideg < 0.0f) { ΔM1wt_qh = MathBase.Interpolate(ideg, m10, m1n10, 0.0f, -10f); }
                if (ideg >= 0.0f && ideg <= 10) { ΔM1wt_qh = MathBase.Interpolate(ideg, m1p10, m10, 10.0f, 0.0f); }
                double ΔM2wt_qh = ΔM2wt.Evaluate((float)ψwtdeg);

                double nwan20 = Nwt_N20.Evaluate((float)ψwtdeg);
                double nwan10 = Nwt_N10.Evaluate((float)ψwtdeg);
                double nw1a0 = Nwt_0.Evaluate((float)ψwtdeg);
                double nwpa10 = Nwt_P10.Evaluate((float)ψwtdeg);
                double Nwt_hq = 0;


                double afx = αfldeg;
                if (afx < -20) { afx = -20f; }
                if (afx > 10) { afx = 10f; }
                if (afx >= -20 && afx < -10.0f) { Nwt_hq = MathBase.Interpolate(afx, nwan20, nwan10, -20.0f, -10f); }
                if (afx >= -10 && afx < 0.0f) { Nwt_hq = MathBase.Interpolate(afx, nw1a0, nwan10, 0.0f, -10f); }
                if (afx >= 0.0f && afx <= 10) { Nwt_hq = MathBase.Interpolate(afx, nwpa10, nw1a0, 10.0f, 0.0f); }

                double Lwt = Lwthq.Evaluate((float)ψwtdeg) * Q;
                double Mwt = (ΔM1wt_qh + ΔM2wt_qh) * Q;
                double Nwt = Nwt_hq * Q;
                double Ldh = 95.6f * rb * V;
                double Mdh = -218f * qb * V;
                double Ndh = -322f * pb * V;

                double Xfh = force.x;
                double Yfh = force.y;
                double Zfh = force.z;
                double Mx = Lwt + (Zfh * m_fuse.y) - (Yfh * m_fuse.z) + Ldh;
                double My = Mwt + (Xfh * m_fuse.z) - (Zfh * m_fuse.x) + Mdh + (kfe * Thrust);
                double Mz = Nwt + (Yfh * m_fuse.x) - (Xfh * m_fuse.y) + Ndh;
                moment = new Vector(Mx, My, Mz);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class AH1 : Fuselage
        {
            [Header("Main Rotor Data")]
            public double CT;
            public double λ, μ, vi;
            public double Thrust;

            private readonly double STAf = 5.08;
            private readonly double WLf = 1.3716;
            private readonly double STAcg = 4.978;
            private readonly double WLcg = 1.854;

            private AnimationCurve Dq;
            private AnimationCurve Lq;
            private AnimationCurve Yq;
            private AnimationCurve ΔDq;
            private AnimationCurve ΔLq;
            private AnimationCurve Mq;
            private AnimationCurve ΔMq;
            private AnimationCurve lq;
            private AnimationCurve Nq;

            /// <summary>
            /// 
            /// </summary>
            public override void Initialize()
            {
                Dq = new AnimationCurve();
                Dq.AddKey(new Keyframe(-90.2562089964221f, 150.12892211691295385f));
                Dq.AddKey(new Keyframe(-77.28210461594814f, 143.06992929233468904f));
                Dq.AddKey(new Keyframe(-68.82387184971515f, 131.0984216688579801f));
                Dq.AddKey(new Keyframe(-61.19555477064159f, 117.07462569415829962f));
                Dq.AddKey(new Keyframe(-55.61533983503723f, 101.41611559166161999f));
                Dq.AddKey(new Keyframe(-49.63743516283692f, 84.52162764990221055f));
                Dq.AddKey(new Keyframe(-43.650111523190915f, 68.8610244435287229f));
                Dq.AddKey(new Keyframe(-38.038500029434275f, 57.3154634589849998f));
                Dq.AddKey(new Keyframe(-28.77232916674842f, 44.517179804163991186f));
                Dq.AddKey(new Keyframe(-19.105328911651384f, 30.8942132218755653f));
                Dq.AddKey(new Keyframe(-8.5801560670578f, 23.0251891969676308f));
                Dq.AddKey(new Keyframe(8.5560853724744f, 27.87281777568469558f));
                Dq.AddKey(new Keyframe(22.892800376758714f, 39.31581667026421f));
                Dq.AddKey(new Keyframe(30.318086379780624f, 52.02828306613554036f));
                Dq.AddKey(new Keyframe(39.4032037571215f, 68.845326164452595f));
                Dq.AddKey(new Keyframe(48.0780727746054f, 85.253167454851123f));
                Dq.AddKey(new Keyframe(55.547313959040565f, 103.723762615856595f));
                Dq.AddKey(new Keyframe(63.41424488007169f, 120.95837993759935764f));
                Dq.AddKey(new Keyframe(69.6433220174905f, 136.9674849394635343f));
                Dq.AddKey(new Keyframe(80.72002773362635f, 148.0159337532623252f));
                Dq.AddKey(new Keyframe(87.26516355644515f, 152.09539320918615f));

                Lq = new AnimationCurve();
                Lq.AddKey(new Keyframe(-91.14529645512845f, -26.339064405712705872f));
                Lq.AddKey(new Keyframe(-86.7419443679427f, -37.01223590510288454056f));
                Lq.AddKey(new Keyframe(-82.34698561330342f, -48.5037573277727369653f));
                Lq.AddKey(new Keyframe(-77.12318526970137f, -59.183223826572765795f));
                Lq.AddKey(new Keyframe(-71.09572333477591f, -71.505685171342673f));
                Lq.AddKey(new Keyframe(-61.35526091461094f, -81.810599205256317943f));
                Lq.AddKey(new Keyframe(-51.12903437331967f, -84.753511429358262393f));
                Lq.AddKey(new Keyframe(-40.438027042268274f, -82.3802966518471636194f));
                Lq.AddKey(new Keyframe(-32.961666076510454f, -73.4351024904591356835f));
                Lq.AddKey(new Keyframe(-28.75031147132495f, -62.828028484872297076f));
                Lq.AddKey(new Keyframe(-24.05738941128638f, -45.268127631112992414f));
                Lq.AddKey(new Keyframe(-18.167368296809187f, -30.9910689695872631352f));
                Lq.AddKey(new Keyframe(-11.02988813261463f, -15.08675296061687765402f));
                Lq.AddKey(new Keyframe(-4.3278120942675855f, -1.634339221780663877184f));
                Lq.AddKey(new Keyframe(-0.19619414827343462f, 1.1984105126490476613f));
                Lq.AddKey(new Keyframe(6.112444426958348f, 16.2906715977495657f));
                Lq.AddKey(new Keyframe(16.91256508111374f, 29.30243537789672811f));
                Lq.AddKey(new Keyframe(34.68229924853446f, 41.85151670142036626f));
                Lq.AddKey(new Keyframe(51.9914492924683f, 49.493645984970743f));
                Lq.AddKey(new Keyframe(65.53304218961065f, 49.7989534563481661f));
                Lq.AddKey(new Keyframe(77.7558327103907f, 41.52102923240356f));
                Lq.AddKey(new Keyframe(87.82678259957251f, 23.4386434276272278f));


                Yq = new AnimationCurve();
                Yq.AddKey(new Keyframe(-89.38739877780787f, -37.14560944038047f));
                Yq.AddKey(new Keyframe(-83.85321653270718f, -52.15147957494207f));
                Yq.AddKey(new Keyframe(-78.7573376682019f, -67.15253318883771f));
                Yq.AddKey(new Keyframe(-71.46994190071948f, -82.17766940606279f));
                Yq.AddKey(new Keyframe(-61.1144224690689f, -97.23652126794906f));
                Yq.AddKey(new Keyframe(-50.65293958276888f, -102.62620789307323f));
                Yq.AddKey(new Keyframe(-41.34260513561517f, -93.05818959029472f));
                Yq.AddKey(new Keyframe(-31.95520635780727f, -76.45805111532556f));
                Yq.AddKey(new Keyframe(-23.449230861856137f, -60.287787109786564f));
                Yq.AddKey(new Keyframe(-18.459315452000396f, -44.958005960444325f));
                Yq.AddKey(new Keyframe(-11.253800535837911f, -27.454769860621923f));
                Yq.AddKey(new Keyframe(-4.953791504861698f, -12.578945784039263f));
                Yq.AddKey(new Keyframe(0.01204130166470918f, 0.5532978114934224f));
                Yq.AddKey(new Keyframe(8.108612541015674f, 19.365423402269798f));
                Yq.AddKey(new Keyframe(17.115506186218767f, 41.244468527047786f));
                Yq.AddKey(new Keyframe(26.584785815346635f, 65.31623468496946f));
                Yq.AddKey(new Keyframe(30.197176314759645f, 74.94686775640449f));
                Yq.AddKey(new Keyframe(39.12218910864266f, 89.35428519822995f));
                Yq.AddKey(new Keyframe(49.795598904241615f, 103.30292904662996f));
                Yq.AddKey(new Keyframe(59.842861013275524f, 100.11559649598124f));
                Yq.AddKey(new Keyframe(67.1543393840874f, 87.28799783256571f));
                Yq.AddKey(new Keyframe(74.8848550528312f, 72.69755260543666f));
                Yq.AddKey(new Keyframe(82.59128811824559f, 55.909569824498035f));
                Yq.AddKey(new Keyframe(88.12065384268038f, 40.464192179174574f));


                ΔDq = new AnimationCurve();
                ΔDq.AddKey(new Keyframe(-90.07482629609834f, 170.78233564938535f));
                ΔDq.AddKey(new Keyframe(-76.52592196686263f, 167.48610368786743f));
                ΔDq.AddKey(new Keyframe(-66.59005879208978f, 155.46886691608765f));
                ΔDq.AddKey(new Keyframe(-57.64831640833776f, 133.80946018172097f));
                ΔDq.AddKey(new Keyframe(-51.28808123997861f, 116.14377338321752f));
                ΔDq.AddKey(new Keyframe(-47.11918760021378f, 98.51416354890432f));
                ΔDq.AddKey(new Keyframe(-40.36878674505611f, 76.89083377872791f));
                ΔDq.AddKey(new Keyframe(-37.487974345269905f, 61.477552111170496f));
                ΔDq.AddKey(new Keyframe(-31.127739176910737f, 43.81186531266701f));
                ΔDq.AddKey(new Keyframe(-25.200427578834834f, 26.59233030464989f));
                ΔDq.AddKey(new Keyframe(-16.178514163548883f, 11.51696953500803f));
                ΔDq.AddKey(new Keyframe(-7.536076964190244f, 1.2771245323356482f));
                ΔDq.AddKey(new Keyframe(-0.5291288081239998f, 0.7227418492784352f));
                ΔDq.AddKey(new Keyframe(9.15553180117584f, 4.075494388027778f));
                ΔDq.AddKey(new Keyframe(18.476750400855167f, 13.580571886691615f));
                ΔDq.AddKey(new Keyframe(25.2485301977552f, 29.712987707108482f));
                ΔDq.AddKey(new Keyframe(31.603420630678812f, 47.608364510956704f));
                ΔDq.AddKey(new Keyframe(37.103153393907036f, 67.27391769107427f));
                ΔDq.AddKey(new Keyframe(42.17530732228758f, 87.82455905932655f));
                ΔDq.AddKey(new Keyframe(47.685729556387f, 108.36798503474076f));
                ΔDq.AddKey(new Keyframe(55.42490646712989f, 131.94788882950292f));
                ΔDq.AddKey(new Keyframe(62.21806520577232f, 149.8360502405131f));
                ΔDq.AddKey(new Keyframe(74.25975414216998f, 166.75975414216995f));
                ΔDq.AddKey(new Keyframe(84.81560662747194f, 169.65913949759485f));


                ΔLq = new AnimationCurve();
                ΔLq.AddKey(new Keyframe(-29.644152555104842f, 29.855557701800706f));
                ΔLq.AddKey(new Keyframe(-27.748159594788916f, 26.031163479492633f));
                ΔLq.AddKey(new Keyframe(-26.544545317966218f, 23.43356298156376f));
                ΔLq.AddKey(new Keyframe(-25.173953168930957f, 20.186723327538477f));
                ΔLq.AddKey(new Keyframe(-22.742257420642584f, 16.36168523168717f));
                ΔLq.AddKey(new Keyframe(-21.185371193097676f, 13.547313974202137f));
                ΔLq.AddKey(new Keyframe(-18.911639087416564f, 10.876311892344344f));
                ΔLq.AddKey(new Keyframe(-15.928787586118084f, 7.915995965059135f));
                ΔLq.AddKey(new Keyframe(-12.936921855214301f, 5.460476895671029f));
                ΔLq.AddKey(new Keyframe(-9.947631618483463f, 2.8607301525980375f));
                ΔLq.AddKey(new Keyframe(-5.856029875732403f, 1.9904277466572253f));
                ΔLq.AddKey(new Keyframe(-3.374111991071622f, 0.977829287661244f));
                ΔLq.AddKey(new Keyframe(-0.5311956731697762f, 0.18114309016375785f));
                ΔLq.AddKey(new Keyframe(4.3171721073981075f, 1.6897388019659658f));
                ΔLq.AddKey(new Keyframe(7.744725602558326f, 3.632734530938123f));
                ΔLq.AddKey(new Keyframe(11.358573176228191f, 6.0081986564505385f));
                ΔLq.AddKey(new Keyframe(13.908741656472003f, 8.817633550104095f));
                ΔLq.AddKey(new Keyframe(16.62975125018781f, 11.194170798188573f));
                ΔLq.AddKey(new Keyframe(19.5434936578456f, 14.363745627025523f));
                ΔLq.AddKey(new Keyframe(21.744253428626635f, 17.606292790762563f));
                ΔLq.AddKey(new Keyframe(24.48586697572597f, 21.13665142832615f));
                ΔLq.AddKey(new Keyframe(26.33077930161184f, 24.451741677934457f));
                ΔLq.AddKey(new Keyframe(28.884811023115077f, 27.477518082115346f));
                ΔLq.AddKey(new Keyframe(30.185006331423196f, 30.288455347369784f));


                Mq = new AnimationCurve();
                Mq.AddKey(new Keyframe(-90.43139142994704f, -202.64805007221958f));
                Mq.AddKey(new Keyframe(-86.47376023110255f, -294.36687530091467f));
                Mq.AddKey(new Keyframe(-82.07799711121811f, -403.5387578237842f));
                Mq.AddKey(new Keyframe(-76.79922965816081f, -517.116032739528f));
                Mq.AddKey(new Keyframe(-67.54935002407318f, -661.4106884930188f));
                Mq.AddKey(new Keyframe(-55.633124699085215f, -740.4910929224843f));
                Mq.AddKey(new Keyframe(-39.284545016851226f, -763.1680308136738f));
                Mq.AddKey(new Keyframe(-26.902262879152616f, -733.3413577274912f));
                Mq.AddKey(new Keyframe(-21.13914299470389f, -655.2238805970146f));
                Mq.AddKey(new Keyframe(-14.478574867597501f, -516.1531054405391f));
                Mq.AddKey(new Keyframe(-9.139142994703917f, -355.22388059701507f));
                Mq.AddKey(new Keyframe(-2.0211844005777664f, -146.46124217621582f));
                Mq.AddKey(new Keyframe(6.857968223399126f, 31.70438131921037f));
                Mq.AddKey(new Keyframe(12.207029369282594f, 236.20606644198347f));
                Mq.AddKey(new Keyframe(19.316321617717847f, 405.7534906114588f));
                Mq.AddKey(new Keyframe(25.544535387578208f, 588.4207992296581f));
                Mq.AddKey(new Keyframe(30.000962927298986f, 753.7554164660568f));
                Mq.AddKey(new Keyframe(43.27395281656234f, 814.0346653827635f));
                Mq.AddKey(new Keyframe(56.529610014443904f, 795.8834857968221f));
                Mq.AddKey(new Keyframe(64.46605681271066f, 708.3052479537794f));
                Mq.AddKey(new Keyframe(73.72075108329324f, 585.7968223399131f));
                Mq.AddKey(new Keyframe(79.87578237843044f, 437.3134328358208f));
                Mq.AddKey(new Keyframe(86.90707751564756f, 253.92392874337975f));


                ΔMq = new AnimationCurve();
                ΔMq.AddKey(new Keyframe(-29.999999999999993f, 180f));
                ΔMq.AddKey(new Keyframe(-28.57019990096171f, 166.4906565195861f));
                ΔMq.AddKey(new Keyframe(-26.724439000234558f, 150.36357476087468f));
                ΔMq.AddKey(new Keyframe(-25.10229612447548f, 132.9370064374886f));
                ΔMq.AddKey(new Keyframe(-23.264354035810157f, 115.50574682686543f));
                ΔMq.AddKey(new Keyframe(-20.997419792019592f, 97.63037869113086f));
                ΔMq.AddKey(new Keyframe(-19.15687143266699f, 80.63384503114494f));
                ΔMq.AddKey(new Keyframe(-17.50345330866063f, 68.42398811540565f));
                ΔMq.AddKey(new Keyframe(-15.447105736401781f, 51.422763168182655f));
                ΔMq.AddKey(new Keyframe(-12.735541713362345f, 35.71164221116004f));
                ΔMq.AddKey(new Keyframe(-10.021371419635635f, 20.43524720477467f));
                ΔMq.AddKey(new Keyframe(-4.928197242565609f, 5.9766999400557665f));
                ΔMq.AddKey(new Keyframe(0.21710234824989527f, 0.21267168808154224f));
                ΔMq.AddKey(new Keyframe(5.456227683807249f, 10.09877765904767f));
                ΔMq.AddKey(new Keyframe(9.826943626365058f, 19.134196877687714f));
                ΔMq.AddKey(new Keyframe(12.760040657822735f, 40.374781724829944f));
                ΔMq.AddKey(new Keyframe(15.253720451406089f, 60.32057129453466f));
                ΔMq.AddKey(new Keyframe(18.389585342333667f, 79.38283510125362f));
                ΔMq.AddKey(new Keyframe(21.327894915165892f, 101.4928718496703f));
                ΔMq.AddKey(new Keyframe(23.8372123328729f, 124.04701712319839f));
                ΔMq.AddKey(new Keyframe(25.907112512705588f, 145.306367119289f));
                ΔMq.AddKey(new Keyframe(28.182386822695413f, 164.82212202559356f));
                ΔMq.AddKey(new Keyframe(30.002606270687288f, 180.43472595063722f));


                lq = new AnimationCurve();
                lq.AddKey(new Keyframe(-89.01582608111285f, 99.9966398978529f));
                lq.AddKey(new Keyframe(-79.77420113571452f, 99.39719767480932f));
                lq.AddKey(new Keyframe(-67.27999731191827f, 100.14582843318438f));
                lq.AddKey(new Keyframe(-52.457914720607505f, 102.25597258156648f));
                lq.AddKey(new Keyframe(-38.0766775309969f, 106.23298948288028f));
                lq.AddKey(new Keyframe(-28.297436242061735f, 112.13736097577367f));
                lq.AddKey(new Keyframe(-24.985719565874774f, 118.60085346594536f));
                lq.AddKey(new Keyframe(-22.86751117233962f, 101.82587950673701f));
                lq.AddKey(new Keyframe(-20.711669634756873f, 88.30617250764423f));
                lq.AddKey(new Keyframe(-19.502032861798966f, 72.93975336850241f));
                lq.AddKey(new Keyframe(-17.8515506871409f, 55.706461476428885f));
                lq.AddKey(new Keyframe(-16.17956385874129f, 40.33332213299285f));
                lq.AddKey(new Keyframe(-13.13127919088737f, 24.009945902355454f));
                lq.AddKey(new Keyframe(-11.443163872181685f, 10.031920970397522f));
                lq.AddKey(new Keyframe(-10.64211552031179f, -0.677396592856411f));
                lq.AddKey(new Keyframe(-0.008064245153008187f, -0.8319612916232586f));
                lq.AddKey(new Keyframe(9.238936863680692f, -0.9663653775074579f));
                lq.AddKey(new Keyframe(10.927052182386376f, -14.944390309465405f));
                lq.AddKey(new Keyframe(14.883908470817573f, -32.67632136016931f));
                lq.AddKey(new Keyframe(15.152716642585972f, -49.42441450220085f));
                lq.AddKey(new Keyframe(18.195625147004506f, -66.21282886999762f));
                lq.AddKey(new Keyframe(19.389133429656255f, -82.97436242061758f));
                lq.AddKey(new Keyframe(21.969691878633142f, -99.75605658412013f));
                lq.AddKey(new Keyframe(24.050267128120737f, -119.78629750344408f));
                lq.AddKey(new Keyframe(30.630691173011712f, -110.57961762037564f));
                lq.AddKey(new Keyframe(39.92070158932836f, -106.99371660898493f));
                lq.AddKey(new Keyframe(53.83958872349723f, -103.00997950337688f));
                lq.AddKey(new Keyframe(67.73697120392467f, -100.88639494640634f));
                lq.AddKey(new Keyframe(87.16642585934619f, -100.23856725244445f));


                Nq = new AnimationCurve();
                Nq.AddKey(new Keyframe(-89.3460721868365f, 446.89490445859866f));
                Nq.AddKey(new Keyframe(-84.35987261146494f, 421.0589171974522f));
                Nq.AddKey(new Keyframe(-76.21974522292993f, 371.8550955414012f));
                Nq.AddKey(new Keyframe(-67.17940552016984f, 315.64490445859866f));
                Nq.AddKey(new Keyframe(-59.05201698513798f, 261.8232484076433f));
                Nq.AddKey(new Keyframe(-52.795116772823775f, 196.61624203821657f));
                Nq.AddKey(new Keyframe(-47.90445859872611f, 136.14649681528658f));
                Nq.AddKey(new Keyframe(-40.74097664543521f, 66.24203821656045f));
                Nq.AddKey(new Keyframe(-35.89490445859872f, -10.39012738853512f));
                Nq.AddKey(new Keyframe(-31.112526539278093f, -110.11146496815286f));
                Nq.AddKey(new Keyframe(-26.24097664543521f, -177.50796178343955f));
                Nq.AddKey(new Keyframe(-20.897027600849214f, -240.32643312101925f));
                Nq.AddKey(new Keyframe(-12.061571125265345f, -204.140127388535f));
                Nq.AddKey(new Keyframe(-7.261146496815286f, -130.65286624203827f));
                Nq.AddKey(new Keyframe(-3.3545647558386236f, -47.85031847133769f));
                Nq.AddKey(new Keyframe(-0.9097664543523933f, 5.0557324840764295f));
                Nq.AddKey(new Keyframe(4.420382165605105f, 103.90127388535029f));
                Nq.AddKey(new Keyframe(10.587048832271819f, 172.6512738853503f));
                Nq.AddKey(new Keyframe(16.715498938428937f, 227.54777070063687f));
                Nq.AddKey(new Keyframe(25.774946921443757f, 178.26433121019102f));
                Nq.AddKey(new Keyframe(35.63906581740983f, 87.34076433121015f));
                Nq.AddKey(new Keyframe(45.08174097664550f, 10.31050955414014f));
                Nq.AddKey(new Keyframe(52.68577494692147f, -66.56050955414014f));
                Nq.AddKey(new Keyframe(61.662420382165635f, -145.859872611465f));
                Nq.AddKey(new Keyframe(70.6390658174098f, -225.15923566878985f));
                Nq.AddKey(new Keyframe(78.65817409766458f, -318.23248407643314f));
                Nq.AddKey(new Keyframe(87.62845010615717f, -399.84076433121027f));

                MathBase.LinearizeCurve(Nq);
                MathBase.LinearizeCurve(lq);
                MathBase.LinearizeCurve(ΔMq);
                MathBase.LinearizeCurve(Mq);
                MathBase.LinearizeCurve(ΔLq);
                MathBase.LinearizeCurve(ΔDq);
                MathBase.LinearizeCurve(Yq);
                MathBase.LinearizeCurve(Dq);
                MathBase.LinearizeCurve(Lq);
            }
            /// <summary>
            /// 
            /// </summary>
            public override void Compute()
            {
                double ꭓ = Math.Atan(μ / -λ);
                double wif_vi = 1.299 + (0.671 * ꭓ) - (1.172 * ꭓ * ꭓ) + (0.351 * ꭓ * ꭓ * ꭓ);
                double ѡɪF = wif_vi * vi;

                double uf = ub;
                double vf = vb - (rb * (STAf - STAcg));
                double wf = wb + (qb * (STAf - STAcg)) - ѡɪF;
                double Vf = Math.Sqrt((uf * uf) + (vf * vf) + (wf * wf));
                double Vfx = Math.Sqrt((uf * uf) + (wf * wf));

                double αF = Math.Atan(wf / Math.Abs(uf)) * Mathf.Rad2Deg;
                double βF = Math.Atan(vf / Vfx);
                double ψw = -βF * Mathf.Rad2Deg;
                if (double.IsNaN(αF) || double.IsInfinity(αF)) { αF = 0.0; }
                if (double.IsNaN(βF) || double.IsInfinity(βF)) { βF = 0.0; }
                if (double.IsNaN(ψw) || double.IsInfinity(ψw)) { ψw = 0.0; }

                double m_Dq = Dq.Evaluate((float)αF) * 0.0929;
                double m_Lq = Lq.Evaluate((float)αF) * 0.0929;
                double m_Mq = Mq.Evaluate((float)αF) * 0.0929;
                double m_ΔDq = ΔDq.Evaluate((float)ψw) * 0.0929;
                double m_ΔLq = ΔLq.Evaluate((float)ψw) * 0.0929;
                double m_ΔMq = ΔMq.Evaluate((float)ψw) * 0.0929;
                double m_Yq = Yq.Evaluate((float)ψw) * 0.0929;
                double m_lq = lq.Evaluate((float)ψw) * 0.0929;
                double m_Nq = Nq.Evaluate((float)ψw) * 0.0929;

                double Q = 0.5 * ρ * Vf * Vf;
                double m_D = (m_Dq * m_ΔDq) * Q;
                double m_Y = m_Yq * Q;
                double m_L = (m_Lq + m_ΔLq) * Q;
                double m_l = m_lq * Q;
                double m_M = (m_Mq + m_ΔMq) * Q;
                double m_N = m_Nq * Q;

                double cosαf = Math.Cos(αF * Mathf.Deg2Rad);
                double sinαf = Math.Sin(αF * Mathf.Deg2Rad);
                double cosβf = Math.Cos(βF);
                double sinβf = Math.Sin(βF);

                double Xf = (-m_D * cosβf * cosαf) - (m_Y * sinβf * sinαf) + (m_L * sinαf);
                double Yf = (m_Y * cosβf) - (m_D * sinβf);
                double Zf = (m_L * cosαf) - (m_D * cosβf * sinαf) - (m_Y * sinαf * sinβf);
                force = new Vector(Xf, Yf, Zf);

                double lf = (m_l * cosαf * cosβf) - (m_M * sinβf * cosαf) - (m_N * sinαf) + (Yf * WLf - WLcg) - (Zf * 0);
                double Mf = (m_M * cosβf) + (m_l * sinβf) - (Xf * (WLf - WLcg)) + (Zf * (STAf - STAcg));
                double Nf = (m_N * cosαf) + (m_l * cosβf * cosαf) - (m_M * sinβf * sinαf) + (Yf * (STAcg - STAf)) + (Xf * 0);
                moment = new Vector(lf, Mf, Nf);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class CustomHeliFuse : Fuselage
        {
            public double S = 962.1;
            public double Rb = 17.5;
            public double αF;
            public double βF;

            [Header("Plots")]
            public AnimationCurve CXcurve;
            public AnimationCurve CYcurve;
            public AnimationCurve CZcurve;

            public AnimationCurve Clcurve;
            public AnimationCurve Cmcurve;
            public AnimationCurve Cncurve;

            [Header("Coefficients")]
            public double Cx, Cy, Cz;
            public double Cl, Cm, Cn;


            /// <summary>
            /// 
            /// </summary>
            public override void Initialize()
            {
                #region CX
                CXcurve = new AnimationCurve();
                CXcurve.AddKey(new Keyframe(-180, 0.015171407f));
                CXcurve.AddKey(new Keyframe(-170, 0.014382999f));
                CXcurve.AddKey(new Keyframe(-160, 0.016478778f));
                CXcurve.AddKey(new Keyframe(-150, 0.021381365f));
                CXcurve.AddKey(new Keyframe(-140, 0.030120249f));
                CXcurve.AddKey(new Keyframe(-130, 0.040717986f));
                CXcurve.AddKey(new Keyframe(-120, 0.050689219f));
                CXcurve.AddKey(new Keyframe(-110, 0.068953281f));
                CXcurve.AddKey(new Keyframe(-100, 0.066202219f));
                CXcurve.AddKey(new Keyframe(-90, 0.064639886f));
                CXcurve.AddKey(new Keyframe(-80, 0.066057615f));
                CXcurve.AddKey(new Keyframe(-70, 0.063884995f));
                CXcurve.AddKey(new Keyframe(-60, 0.072940828f));
                CXcurve.AddKey(new Keyframe(-50, 0.083330081f));
                CXcurve.AddKey(new Keyframe(-45, 0.076841673f));
                CXcurve.AddKey(new Keyframe(-40, 0.061033527f));
                CXcurve.AddKey(new Keyframe(-35, 0.047823497f));
                CXcurve.AddKey(new Keyframe(-30, 0.04297272f));
                CXcurve.AddKey(new Keyframe(-24, 0.031520105f));
                CXcurve.AddKey(new Keyframe(-22, 0.028793187f));
                CXcurve.AddKey(new Keyframe(-20, 0.02607793f));
                CXcurve.AddKey(new Keyframe(-18, 0.024066666f));
                CXcurve.AddKey(new Keyframe(-16, 0.022130643f));
                CXcurve.AddKey(new Keyframe(-14, 0.020118413f));
                CXcurve.AddKey(new Keyframe(-12, 0.018190555f));
                CXcurve.AddKey(new Keyframe(-10, 0.01699859f));
                CXcurve.AddKey(new Keyframe(-8, 0.016312035f));
                CXcurve.AddKey(new Keyframe(-6, 0.015526748f));
                CXcurve.AddKey(new Keyframe(-4, 0.0148211f));
                CXcurve.AddKey(new Keyframe(-2, 0.014323442f));
                CXcurve.AddKey(new Keyframe(0, 0.014324087f));
                CXcurve.AddKey(new Keyframe(2, 0.014528953f));
                CXcurve.AddKey(new Keyframe(4, 0.01417045f));
                CXcurve.AddKey(new Keyframe(6, 0.014231247f));
                CXcurve.AddKey(new Keyframe(8, 0.01407734f));
                CXcurve.AddKey(new Keyframe(10, 0.014151371f));
                CXcurve.AddKey(new Keyframe(12, 0.014075671f));
                CXcurve.AddKey(new Keyframe(14, 0.014196087f));
                CXcurve.AddKey(new Keyframe(16, 0.014111734f));
                CXcurve.AddKey(new Keyframe(18, 0.014600557f));
                CXcurve.AddKey(new Keyframe(20, 0.015208112f));
                CXcurve.AddKey(new Keyframe(22, 0.016031584f));
                CXcurve.AddKey(new Keyframe(24, 0.01698443f));
                CXcurve.AddKey(new Keyframe(30, 0.019608551f));
                CXcurve.AddKey(new Keyframe(35, 0.022338574f));
                CXcurve.AddKey(new Keyframe(40, 0.02453256f));
                CXcurve.AddKey(new Keyframe(45, 0.029059432f));
                CXcurve.AddKey(new Keyframe(50, 0.033570602f));
                CXcurve.AddKey(new Keyframe(60, 0.049952501f));
                CXcurve.AddKey(new Keyframe(70, 0.064535564f));
                CXcurve.AddKey(new Keyframe(80, 0.068437739f));
                CXcurve.AddKey(new Keyframe(90, 0.079056095f));
                CXcurve.AddKey(new Keyframe(100, 0.081206485f));
                CXcurve.AddKey(new Keyframe(110, 0.076937401f));
                CXcurve.AddKey(new Keyframe(120, 0.072527055f));
                CXcurve.AddKey(new Keyframe(130, 0.060199943f));
                CXcurve.AddKey(new Keyframe(140, 0.047856245f));
                CXcurve.AddKey(new Keyframe(150, 0.035778919f));
                CXcurve.AddKey(new Keyframe(160, 0.03019726f));
                CXcurve.AddKey(new Keyframe(170, 0.019617256f));
                CXcurve.AddKey(new Keyframe(180, 0.015171407f));
                MathBase.LinearizeCurve(CXcurve);

                #endregion

                #region CY

                CYcurve = new AnimationCurve();
                AddKey(CYcurve, -180, -0.002783608);
                AddKey(CYcurve, -170, 0.001759013);
                AddKey(CYcurve, -160, 0.010104415);
                AddKey(CYcurve, -150, 0.019269216);
                AddKey(CYcurve, -140, 0.026664539);
                AddKey(CYcurve, -130, 0.031051962);
                AddKey(CYcurve, -120, 0.032794024);
                AddKey(CYcurve, -110, 0.031989631);
                AddKey(CYcurve, -100, 0.022218898);
                AddKey(CYcurve, -90, 0.015887815);
                AddKey(CYcurve, -80, 0.006368649);
                AddKey(CYcurve, -70, -0.001623816);
                AddKey(CYcurve, -60, -0.023767404);
                AddKey(CYcurve, -50, -0.047049321);
                AddKey(CYcurve, -45, -0.050493558);
                AddKey(CYcurve, -40, -0.043518928);
                AddKey(CYcurve, -35, -0.037327107);
                AddKey(CYcurve, -30, -0.03665064);
                AddKey(CYcurve, -24, -0.027116422);
                AddKey(CYcurve, -22, -0.024338348);
                AddKey(CYcurve, -20, -0.021467907);
                AddKey(CYcurve, -18, -0.019222554);
                AddKey(CYcurve, -16, -0.016872649);
                AddKey(CYcurve, -14, -0.014193974);
                AddKey(CYcurve, -12, -0.010662314);
                AddKey(CYcurve, -10, -0.008292974);
                AddKey(CYcurve, -8, -0.007170474);
                AddKey(CYcurve, -6, -0.005405068);
                AddKey(CYcurve, -4, -0.003291717);
                AddKey(CYcurve, -2, -0.001437621);
                AddKey(CYcurve, 0, -0.000635895);
                AddKey(CYcurve, 2, 2.53658E-05);
                AddKey(CYcurve, 4, -6.85299E-05);
                AddKey(CYcurve, 6, 0.000228619);
                AddKey(CYcurve, 8, 0.000226239);
                AddKey(CYcurve, 10, 0.000884218);
                AddKey(CYcurve, 12, 0.000558202);
                AddKey(CYcurve, 14, 0.00102504);
                AddKey(CYcurve, 16, 0.001864894);
                AddKey(CYcurve, 18, 0.002777948);
                AddKey(CYcurve, 20, 0.003975324);
                AddKey(CYcurve, 22, 0.004673032);
                AddKey(CYcurve, 24, 0.005964544);
                AddKey(CYcurve, 30, 0.008385566);
                AddKey(CYcurve, 35, 0.010952241);
                AddKey(CYcurve, 40, 0.011936672);
                AddKey(CYcurve, 45, 0.014449723);
                AddKey(CYcurve, 50, 0.014716405);
                AddKey(CYcurve, 60, 0.025497607);
                AddKey(CYcurve, 70, 0.029206917);
                AddKey(CYcurve, 80, 0.018341167);
                AddKey(CYcurve, 90, 0.000560997);
                AddKey(CYcurve, 100, -0.010914032);
                AddKey(CYcurve, 110, -0.020765828);
                AddKey(CYcurve, 120, -0.02819244);
                AddKey(CYcurve, 130, -0.027088145);
                AddKey(CYcurve, 140, -0.027164313);
                AddKey(CYcurve, 150, -0.020517544);
                AddKey(CYcurve, 160, -0.017579931);
                AddKey(CYcurve, 170, -0.007283779);
                AddKey(CYcurve, 180, -0.002783608);


                #endregion

                #region CZ

                CZcurve = new AnimationCurve();
                AddKey(CZcurve, -180, -0.005245217);
                AddKey(CZcurve, -170, 0.000198648);
                AddKey(CZcurve, -160, -0.000775205);
                AddKey(CZcurve, -150, -0.003549797);
                AddKey(CZcurve, -140, -0.001816508);
                AddKey(CZcurve, -130, 0.006380025);
                AddKey(CZcurve, -120, 0.024699951);
                AddKey(CZcurve, -110, 0.030918309);
                AddKey(CZcurve, -100, 0.037440862);
                AddKey(CZcurve, -90, 0.040738657);
                AddKey(CZcurve, -80, 0.046198896);
                AddKey(CZcurve, -70, 0.048855879);
                AddKey(CZcurve, -60, 0.063376524);
                AddKey(CZcurve, -50, 0.083972949);
                AddKey(CZcurve, -45, 0.079687214);
                AddKey(CZcurve, -40, 0.058865571);
                AddKey(CZcurve, -35, 0.045706947);
                AddKey(CZcurve, -30, 0.044264233);
                AddKey(CZcurve, -24, 0.032780264);
                AddKey(CZcurve, -22, 0.033816229);
                AddKey(CZcurve, -20, 0.032013118);
                AddKey(CZcurve, -18, 0.02749435);
                AddKey(CZcurve, -16, 0.023820908);
                AddKey(CZcurve, -14, 0.019267227);
                AddKey(CZcurve, -12, 0.01001445);
                AddKey(CZcurve, -10, 0.010349118);
                AddKey(CZcurve, -8, 0.010235144);
                AddKey(CZcurve, -6, 0.010371114);
                AddKey(CZcurve, -4, 0.009665444);
                AddKey(CZcurve, -2, 0.008350096);
                AddKey(CZcurve, 0, 0.003029453);
                AddKey(CZcurve, 2, 0.002964566);
                AddKey(CZcurve, 4, 0.003442532);
                AddKey(CZcurve, 6, 0.003642822);
                AddKey(CZcurve, 8, 0.003658568);
                AddKey(CZcurve, 10, 0.002951516);
                AddKey(CZcurve, 12, 0.001606463);
                AddKey(CZcurve, 14, 0.00316268);
                AddKey(CZcurve, 16, 0.004066092);
                AddKey(CZcurve, 18, 0.005115683);
                AddKey(CZcurve, 20, 0.010742582);
                AddKey(CZcurve, 22, 0.007231232);
                AddKey(CZcurve, 24, 0.012923916);
                AddKey(CZcurve, 30, 0.017672339);
                AddKey(CZcurve, 35, 0.011824731);
                AddKey(CZcurve, 40, 0.006228581);
                AddKey(CZcurve, 45, 0.016593333);
                AddKey(CZcurve, 50, 0.00433376);
                AddKey(CZcurve, 60, 0.054451167);
                AddKey(CZcurve, 70, 0.069970242);
                AddKey(CZcurve, 80, 0.070463525);
                AddKey(CZcurve, 90, 0.027625814);
                AddKey(CZcurve, 100, 0.019642351);
                AddKey(CZcurve, 110, 0.010634448);
                AddKey(CZcurve, 120, -0.003653574);
                AddKey(CZcurve, 130, 0.020297526);
                AddKey(CZcurve, 140, -0.001864443);
                AddKey(CZcurve, 150, 0.003952319);
                AddKey(CZcurve, 160, 0.003988951);
                AddKey(CZcurve, 170, -0.0109643);
                AddKey(CZcurve, 180, -0.005245217);


                #endregion


                #region Cxx moments

                Clcurve = new AnimationCurve();
                Cmcurve = new AnimationCurve();
                Cncurve = new AnimationCurve();
                AddKeys(Clcurve, Cmcurve, Cncurve, -180, 0.000453921, -0.002561385, 0.002827014);
                AddKeys(Clcurve, Cmcurve, Cncurve, -170, -5.86071E-05, -0.001942179, 0.000221315);
                AddKeys(Clcurve, Cmcurve, Cncurve, -160, -0.000306473, -0.001640691, -0.001644271);
                AddKeys(Clcurve, Cmcurve, Cncurve, -150, -3.91705E-05, -0.00035027, -0.002871914);
                AddKeys(Clcurve, Cmcurve, Cncurve, -140, -0.000674055, -0.000981706, -0.004062359);
                AddKeys(Clcurve, Cmcurve, Cncurve, -130, -0.002367091, -0.002067141, -0.004735443);
                AddKeys(Clcurve, Cmcurve, Cncurve, -120, -0.003313544, -0.001462218, -0.004875829);
                AddKeys(Clcurve, Cmcurve, Cncurve, -110, -0.002817024, -0.000490003, -0.002633181);
                AddKeys(Clcurve, Cmcurve, Cncurve, -100, -0.002385698, 0.000677766, -0.002567302);
                AddKeys(Clcurve, Cmcurve, Cncurve, -90, -0.000774889, 0.001443359, -0.001500553);
                AddKeys(Clcurve, Cmcurve, Cncurve, -80, -1.90778E-06, 0.001640704, 0.000459746);
                AddKeys(Clcurve, Cmcurve, Cncurve, -70, 0.000458622, 0.001634765, 0.001789215);
                AddKeys(Clcurve, Cmcurve, Cncurve, -60, -0.002555291, 0.003532205, 0.008224582);
                AddKeys(Clcurve, Cmcurve, Cncurve, -50, -0.000933956, 0.00396087, 0.010381634);
                AddKeys(Clcurve, Cmcurve, Cncurve, -45, -0.0015433, 0.004516287, 0.010490727);
                AddKeys(Clcurve, Cmcurve, Cncurve, -40, -0.000154272, 0.00279131, 0.009257808);
                AddKeys(Clcurve, Cmcurve, Cncurve, -35, 0.000692478, 0.001370243, 0.007856607);
                AddKeys(Clcurve, Cmcurve, Cncurve, -30, 0.000325864, 0.001738083, 0.006786634);
                AddKeys(Clcurve, Cmcurve, Cncurve, -24, 0.001356302, -0.000566121, 0.005188073);
                AddKeys(Clcurve, Cmcurve, Cncurve, -22, 0.001505906, -0.001007289, 0.004624744);
                AddKeys(Clcurve, Cmcurve, Cncurve, -20, 0.001736743, -0.001724469, 0.004371794);
                AddKeys(Clcurve, Cmcurve, Cncurve, -18, 0.001547456, -0.001722268, 0.004023989);
                AddKeys(Clcurve, Cmcurve, Cncurve, -16, 0.001510304, -0.001991851, 0.003618348);
                AddKeys(Clcurve, Cmcurve, Cncurve, -14, 0.001409975, -0.002236209, 0.003318769);
                AddKeys(Clcurve, Cmcurve, Cncurve, -12, 0.001186554, -0.002379376, 0.003171733);
                AddKeys(Clcurve, Cmcurve, Cncurve, -10, 0.001303443, -0.003159738, 0.002851161);
                AddKeys(Clcurve, Cmcurve, Cncurve, -8, 0.001168407, -0.003182599, 0.002299713);
                AddKeys(Clcurve, Cmcurve, Cncurve, -6, 0.001075773, -0.003382051, 0.001809355);
                AddKeys(Clcurve, Cmcurve, Cncurve, -4, 0.000929628, -0.003454418, 0.001371408);
                AddKeys(Clcurve, Cmcurve, Cncurve, -2, 0.000763913, -0.003381099, 0.00088299);
                AddKeys(Clcurve, Cmcurve, Cncurve, 0, 0.00046596, -0.002374604, 0.000459189);
                AddKeys(Clcurve, Cmcurve, Cncurve, 2, 0.00035288, -0.002212833, -1.44762E-05);
                AddKeys(Clcurve, Cmcurve, Cncurve, 4, 0.000266419, -0.00218006, -0.000448226);
                AddKeys(Clcurve, Cmcurve, Cncurve, 6, 0.000194335, -0.002451521, -0.000956959);
                AddKeys(Clcurve, Cmcurve, Cncurve, 8, 0.000107957, -0.002735682, -0.001559503);
                AddKeys(Clcurve, Cmcurve, Cncurve, 10, -1.2781E-05, -0.002830047, -0.002128324);
                AddKeys(Clcurve, Cmcurve, Cncurve, 12, -7.68412E-05, -0.002175671, -0.002461616);
                AddKeys(Clcurve, Cmcurve, Cncurve, 14, -0.000177251, -0.002526822, -0.00296694);
                AddKeys(Clcurve, Cmcurve, Cncurve, 16, -0.00029571, -0.002795252, -0.003308027);
                AddKeys(Clcurve, Cmcurve, Cncurve, 18, -0.000407154, -0.002926401, -0.003691693);
                AddKeys(Clcurve, Cmcurve, Cncurve, 20, -0.000560601, -0.003848821, -0.004019955);
                AddKeys(Clcurve, Cmcurve, Cncurve, 22, -0.000743955, -0.003631089, -0.004521243);
                AddKeys(Clcurve, Cmcurve, Cncurve, 24, -0.000852193, -0.004129519, -0.005039844);
                AddKeys(Clcurve, Cmcurve, Cncurve, 30, -0.000504352, -0.00253902, -0.006499494);
                AddKeys(Clcurve, Cmcurve, Cncurve, 35, -0.000888381, -0.002411886, -0.007094446);
                AddKeys(Clcurve, Cmcurve, Cncurve, 40, -8.5952E-05, -0.000655965, -0.007756756);
                AddKeys(Clcurve, Cmcurve, Cncurve, 45, 0.000457702, -0.000390737, -0.00800792);
                AddKeys(Clcurve, Cmcurve, Cncurve, 50, 0.0012194, 0.000761372, -0.008191777);
                AddKeys(Clcurve, Cmcurve, Cncurve, 60, 0.003592148, 0.001110977, -0.009005316);
                AddKeys(Clcurve, Cmcurve, Cncurve, 70, 0.009313404, 0.002958141, -0.008520442);
                AddKeys(Clcurve, Cmcurve, Cncurve, 80, 0.011051116, 0.001914224, -0.005626343);
                AddKeys(Clcurve, Cmcurve, Cncurve, 90, 0.008241619, 0.000451115, -0.00250431);
                AddKeys(Clcurve, Cmcurve, Cncurve, 100, 0.007580027, -0.000842196, -0.000482547);
                AddKeys(Clcurve, Cmcurve, Cncurve, 110, 0.007732088, -0.002056296, 0.001698137);
                AddKeys(Clcurve, Cmcurve, Cncurve, 120, 0.008627765, -0.003676315, 0.003906822);
                AddKeys(Clcurve, Cmcurve, Cncurve, 130, 0.012874061, -0.00861449, 0.006095062);
                AddKeys(Clcurve, Cmcurve, Cncurve, 140, 0.006725971, -0.006406065, 0.006980109);
                AddKeys(Clcurve, Cmcurve, Cncurve, 150, 0.0045844, -0.006296711, 0.007169039);
                AddKeys(Clcurve, Cmcurve, Cncurve, 160, 0.002880563, -0.005758187, 0.00540725);
                AddKeys(Clcurve, Cmcurve, Cncurve, 170, 0.001017537, -0.002886606, 0.004739885);
                AddKeys(Clcurve, Cmcurve, Cncurve, 180, 0.000453906, -0.00256131, 0.002827093);

                #endregion
            }

            public override void Compute()
            {
                double Vf = Math.Sqrt((ub * ub) + (vb * vb) + (wb * wb));
                αF = Math.Atan(wb / Math.Abs(ub)) * Mathf.Rad2Deg;
                βF = Math.Atan(vb / Vf);
                double ψw = -βF * Mathf.Rad2Deg;
                if (double.IsNaN(αF) || double.IsInfinity(αF)) { αF = 0.0; }
                if (double.IsNaN(βF) || double.IsInfinity(βF)) { βF = 0.0; }
                if (double.IsNaN(ψw) || double.IsInfinity(ψw)) { ψw = 0.0; }

                Cx = CXcurve.Evaluate((float)αF);
                Cy = CYcurve.Evaluate((float)αF);
                Cz = CZcurve.Evaluate((float)αF);
                //force = new Vector(Cx, Cy, Cz) * 0.5 * ρ * Vf * Vf * S;

                Cl = Clcurve.Evaluate((float)βF);
                Cm = Cmcurve.Evaluate((float)βF);
                Cn = Cncurve.Evaluate((float)βF);
                //moment = new Vector(Cl, Cm, Cn) * 0.5 * ρ * Vf * Vf * S * Rb;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="curve"></param>
            /// <param name="a"></param>
            /// <param name="b"></param>
            public void AddKey(AnimationCurve curve, double a, double b)
            {
                curve.AddKey(new Keyframe((float)a, (float)b));
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="A"></param>
            /// <param name="B"></param>
            /// <param name="C"></param>
            /// <param name="alpha"></param>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="c"></param>
            public void AddKeys(AnimationCurve A, AnimationCurve B, AnimationCurve C, double alpha, double a, double b, double c)
            {
                A.AddKey(new Keyframe((float)alpha, (float)a));
                B.AddKey(new Keyframe((float)alpha, (float)b));
                C.AddKey(new Keyframe((float)alpha, (float)c));
            }
        }
    }
}
