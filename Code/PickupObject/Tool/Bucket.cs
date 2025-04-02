using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal partial class Bucket : Tool
    {
        [Export]
        public float myMaxCapacity { get; private set; } = 10;

        public override void Use()
        {
            if (MaterialInteractions.Total(ref myContent) > 0.0f)
            {
                Empty();
            }
        }

        public void PutIn(ref MaterialsList someMaterials)
        {
            float currentCapacity = MaterialInteractions.Total(ref myContent);
            float materialsToAddTotal = MaterialInteractions.Total(ref someMaterials);

            if (currentCapacity < myMaxCapacity)
            {
                if (currentCapacity + materialsToAddTotal < myMaxCapacity)
                {
                    foreach (int material in MaterialGroups.All)
                    {
                        myContent[material] += someMaterials[material];
                        someMaterials[material] = 0;
                    }
                }
                else 
                {
                    float toAdd = myMaxCapacity - currentCapacity;
                    float fraction = toAdd / materialsToAddTotal;

                    foreach (int material in MaterialGroups.All)
                    {
                        myContent[material] += someMaterials[material] * fraction;
                        someMaterials[material] = Math.Max(someMaterials[material] - (someMaterials[material] * fraction), 0);
                    }
                }

                GD.Print("Bucket: ", MaterialInteractions.Total(ref myContent), " Shovel: ", MaterialInteractions.Total(ref someMaterials));
            }
        }

        public void Empty()
        {

        }
    }
}
