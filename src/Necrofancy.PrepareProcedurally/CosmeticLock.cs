using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public record CosmeticLock
    {
        public CosmeticLock(Pawn pawn)
        {
            Name = pawn.Name;
            Gender = pawn.gender;
            
            EndoGenes = pawn.genes.Endogenes.ToList();
            XenoGenes = pawn.genes.Xenogenes.ToList();
            GeneOverrides = new Dictionary<GeneDef, GeneDef>();
            
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                if (gene.overriddenByGene is { } overrideGene)
                {
                    GeneOverrides.Add(gene.def, overrideGene.def);
                }
            }
            
            var story = pawn.story;
            
            Head = story.headType;
            Hair = story.hairDef;
            HairColor = story.HairColor;

            Body = story.bodyType;
            Fur = story.furDef;
            SkinColor = story.SkinColorBase;
            SkinColorOverride = pawn.story.skinColorOverride;

            var style = pawn.style;
            FavoriteColor = story.favoriteColor;
            Beard = style.beardDef;
            FaceTattoo = style.FaceTattoo;
            BodyTattoo = style.BodyTattoo;
        }

        public Name Name { get; }
        public Gender Gender { get; }

        public HeadTypeDef Head { get; }
        public HairDef Hair { get; }
        public Color HairColor { get; }
        
        public BodyTypeDef Body { get; }
        public Color SkinColor { get; }
        public Color? SkinColorOverride { get; }
        public FurDef Fur { get; }

        public ColorDef FavoriteColor { get; }
        public BeardDef Beard { get; }
        public TattooDef BodyTattoo { get; }
        public TattooDef FaceTattoo { get; }
        
        public List<Gene> EndoGenes { get; }
        public List<Gene> XenoGenes { get; }
        public Dictionary<GeneDef, GeneDef> GeneOverrides { get; }
        
        public void ApplyToPawn(Pawn pawn)
        {
            pawn.Name = Name;
            pawn.gender = Gender;
            
            foreach (var gene in pawn.genes.GenesListForReading)
            {
                pawn.genes.RemoveGene(gene);
            }

            var addedGenes = new List<Gene>();
            var genesByDef = new Dictionary<GeneDef, Gene>(); 
            foreach (var gene in EndoGenes)
            {
                var applied = pawn.genes.AddGene(gene.def, xenogene: false);
                addedGenes.Add(applied);
                genesByDef[gene.def] = applied;
            }

            foreach (var gene in XenoGenes)
            {
                var applied = pawn.genes.AddGene(gene.def, xenogene: true);
                addedGenes.Add(applied);
                genesByDef[gene.def] = applied;
            }
            
            // restore the gene override order.
            foreach (var gene in addedGenes)
            {
                gene.overriddenByGene = GeneOverrides.TryGetValue(gene.def, out var overrideGene) 
                    ? genesByDef[overrideGene] 
                    : null;
            }
            
            pawn.story.headType = Head;
            pawn.story.hairDef = Hair;
            pawn.story.HairColor = HairColor;
            
            pawn.story.bodyType = Body;
            pawn.story.SkinColorBase = SkinColor;
            pawn.story.furDef = Fur;
            pawn.story.skinColorOverride = SkinColorOverride;
            
            pawn.story.favoriteColor = FavoriteColor;
            pawn.style.beardDef = Beard;
            pawn.style.FaceTattoo = FaceTattoo;
            pawn.style.BodyTattoo = BodyTattoo;
        }
    }