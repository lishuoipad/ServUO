using System;
using System.Collections.Generic;

using Server.Accounting;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.SovereignVendor
{
    public abstract class BaseSovereignVoucher : Item
    {
        private static readonly HashSet<string> RedeemedKeys = new HashSet<string>();
        private const string LedgerPath = "Saves/Misc/SovereignVoucherLedger.bin";

        private int m_Value;
        private string m_OwnerAccount;
        private string m_RedeemKey;

        public int SovereignValue { get { return m_Value; } }

        public static void Configure()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                LedgerPath,
                writer =>
                {
                    writer.Write((int)0); // version
                    writer.Write(RedeemedKeys.Count);

                    foreach (string key in RedeemedKeys)
                    {
                        writer.Write(key);
                    }
                });
        }

        private static void OnWorldLoad()
        {
            Persistence.Deserialize(
                LedgerPath,
                reader =>
                {
                    int version = reader.ReadInt();
                    int count = reader.ReadInt();

                    RedeemedKeys.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();

                        if (!String.IsNullOrEmpty(key))
                        {
                            RedeemedKeys.Add(key);
                        }
                    }
                });
        }

        public BaseSovereignVoucher(int value)
            : base(0x14F0)
        {
            m_Value = value;
            m_RedeemKey = Guid.NewGuid().ToString("N");

            Name = String.Format("{0} sovereign{1} voucher", value, value == 1 ? String.Empty : "s");
            Hue = 0x501;
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public BaseSovereignVoucher(Serial serial)
            : base(serial)
        {
        }

        public override bool DisplayLootType { get { return false; } }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            from.SendMessage("Sovereign vouchers are bound to the account that bought them.");
            return false;
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            if (String.IsNullOrEmpty(m_OwnerAccount))
            {
                Mobile owner = RootParent as Mobile;
                Account account = owner == null ? null : owner.Account as Account;

                if (account != null)
                {
                    m_OwnerAccount = account.Username;
                    InvalidateProperties();
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            PlayerMobile player = from as PlayerMobile;
            Account account = player == null ? null : player.Account as Account;

            if (account == null)
            {
                from.SendMessage("Only player accounts can redeem this voucher.");
                return;
            }

            if (String.IsNullOrEmpty(m_OwnerAccount))
            {
                m_OwnerAccount = account.Username;
            }

            if (!String.Equals(m_OwnerAccount, account.Username, StringComparison.OrdinalIgnoreCase))
            {
                from.SendMessage("This sovereign voucher belongs to another account.");
                return;
            }

            if (String.IsNullOrEmpty(m_RedeemKey))
            {
                m_RedeemKey = Guid.NewGuid().ToString("N");
            }

            if (RedeemedKeys.Contains(m_RedeemKey))
            {
                from.SendMessage("This sovereign voucher has already been redeemed.");
                Delete();
                return;
            }

            if (!player.DepositSovereigns(m_Value))
            {
                from.SendMessage("The sovereigns could not be added to your account.");
                return;
            }

            RedeemedKeys.Add(m_RedeemKey);
            from.SendMessage("You redeem the voucher for {0} sovereign{1}.", m_Value, m_Value == 1 ? String.Empty : "s");
            Delete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(String.Format("Value: {0} sovereign{1}", m_Value, m_Value == 1 ? String.Empty : "s"));

            if (!String.IsNullOrEmpty(m_OwnerAccount))
            {
                list.Add("Account bound");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_Value);
            writer.Write(m_OwnerAccount);
            writer.Write(m_RedeemKey);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Value = reader.ReadInt();
            m_OwnerAccount = reader.ReadString();
            m_RedeemKey = reader.ReadString();
        }
    }

    public class SovereignVoucher1 : BaseSovereignVoucher
    {
        [Constructable]
        public SovereignVoucher1()
            : base(1)
        {
        }

        public SovereignVoucher1(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); }
    }

    public class SovereignVoucher10 : BaseSovereignVoucher
    {
        [Constructable]
        public SovereignVoucher10()
            : base(10)
        {
        }

        public SovereignVoucher10(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); }
    }

    public class SovereignVoucher100 : BaseSovereignVoucher
    {
        [Constructable]
        public SovereignVoucher100()
            : base(100)
        {
        }

        public SovereignVoucher100(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); }
    }

    public class SovereignVoucherVendor : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        [Constructable]
        public SovereignVoucherVendor()
            : base("the sovereign voucher broker")
        {
            Name = "Sovereign Voucher Broker";
            SetSkill(SkillName.ItemID, 60.0, 83.0);
            SetSkill(SkillName.EvalInt, 60.0, 83.0);
        }

        public SovereignVoucherVendor(Serial serial)
            : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.MerchantsGuild; } }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBSovereignVoucherVendor());
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SBSovereignVoucherVendor : SBInfo
    {
        private readonly List<GenericBuyInfo> m_BuyInfo = new InternalBuyInfo();
        private readonly IShopSellInfo m_SellInfo = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get { return m_BuyInfo; } }
        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }

        private class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("1 sovereign voucher", typeof(SovereignVoucher1), 1000, 20, 0x14F0, 0x501));
                Add(new GenericBuyInfo("10 sovereign voucher", typeof(SovereignVoucher10), 10000, 20, 0x14F0, 0x501));
                Add(new GenericBuyInfo("100 sovereign voucher", typeof(SovereignVoucher100), 100000, 20, 0x14F0, 0x501));
            }
        }

        private class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
            }
        }
    }
}
