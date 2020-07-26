using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Zo.Managers
{
    public class TextManager
    {
        #region Constants

        public const int BASE_LETTER_WIDTH = 6;
        public const int BASE_LETTER_X_DISTANCE = 2;
        public const int BASE_LETTER_Y_DISTANCE = 10;
        public const int BASE_LETTER_HEIGHT = 8;

        public const char NEWLINE_CHARACTER = '\n';

        private readonly List<string> EMPTYTEXT = new List<string>();

        #endregion

        #region Constructors

        public TextManager(SizeManager sizes, Action<Action> subscribeToUpdate)
        {
            subscribeToUpdate(this.UpdateState);
            this.Sizes = sizes;
            this.TexturesByCharacter = new Dictionary<char, Texture2D>();
            this.Lines = new List<string>();
        }

        #endregion

        #region Properties

        protected SizeManager Sizes { get; }

        protected Dictionary<char, Texture2D> TexturesByCharacter { get; set; }

        public List<string> Lines { get; set; }

        #endregion

        #region Methods

        public bool Supports(char letter) =>
            this.TexturesByCharacter.ContainsKey(letter);

        public bool Supports(string letterContainer) =>
            this.TexturesByCharacter.ContainsKey(letterContainer[0]);

        public void LoadContent(ContentManager content)
        {
            this.TexturesByCharacter = new Dictionary<char, Texture2D>
            {
                ['a'] = content.Load<Texture2D>("Alphabet/im_a_low"),
                ['b'] = content.Load<Texture2D>("Alphabet/im_b_low"),
                ['c'] = content.Load<Texture2D>("Alphabet/im_c_low"),
                ['d'] = content.Load<Texture2D>("Alphabet/im_d_low"),
                ['e'] = content.Load<Texture2D>("Alphabet/im_e_low"),
                ['f'] = content.Load<Texture2D>("Alphabet/im_f_low"),
                ['g'] = content.Load<Texture2D>("Alphabet/im_g_low"),
                ['h'] = content.Load<Texture2D>("Alphabet/im_h_low"),
                ['i'] = content.Load<Texture2D>("Alphabet/im_i_low"),
                ['j'] = content.Load<Texture2D>("Alphabet/im_j_low"),
                ['k'] = content.Load<Texture2D>("Alphabet/im_k_low"),
                ['l'] = content.Load<Texture2D>("Alphabet/im_l_low"),
                ['m'] = content.Load<Texture2D>("Alphabet/im_m_low"),
                ['n'] = content.Load<Texture2D>("Alphabet/im_n_low"),
                ['o'] = content.Load<Texture2D>("Alphabet/im_o_low"),
                ['p'] = content.Load<Texture2D>("Alphabet/im_p_low"),
                ['q'] = content.Load<Texture2D>("Alphabet/im_q_low"),
                ['r'] = content.Load<Texture2D>("Alphabet/im_r_low"),
                ['s'] = content.Load<Texture2D>("Alphabet/im_s_low"),
                ['t'] = content.Load<Texture2D>("Alphabet/im_t_low"),
                ['u'] = content.Load<Texture2D>("Alphabet/im_u_low"),
                ['v'] = content.Load<Texture2D>("Alphabet/im_v_low"),
                ['w'] = content.Load<Texture2D>("Alphabet/im_w_low"),
                ['x'] = content.Load<Texture2D>("Alphabet/im_x_low"),
                ['y'] = content.Load<Texture2D>("Alphabet/im_y_low"),
                ['z'] = content.Load<Texture2D>("Alphabet/im_z_low"),

                ['A'] = content.Load<Texture2D>("Alphabet/im_a_cap"),
                ['B'] = content.Load<Texture2D>("Alphabet/im_b_cap"),
                ['C'] = content.Load<Texture2D>("Alphabet/im_c_cap"),
                ['D'] = content.Load<Texture2D>("Alphabet/im_d_cap"),
                ['E'] = content.Load<Texture2D>("Alphabet/im_e_cap"),
                ['F'] = content.Load<Texture2D>("Alphabet/im_f_cap"),
                ['G'] = content.Load<Texture2D>("Alphabet/im_g_cap"),
                ['H'] = content.Load<Texture2D>("Alphabet/im_h_cap"),
                ['I'] = content.Load<Texture2D>("Alphabet/im_i_cap"),
                ['J'] = content.Load<Texture2D>("Alphabet/im_j_cap"),
                ['K'] = content.Load<Texture2D>("Alphabet/im_k_cap"),
                ['L'] = content.Load<Texture2D>("Alphabet/im_l_cap"),
                ['M'] = content.Load<Texture2D>("Alphabet/im_m_cap"),
                ['N'] = content.Load<Texture2D>("Alphabet/im_n_cap"),
                ['O'] = content.Load<Texture2D>("Alphabet/im_o_cap"),
                ['P'] = content.Load<Texture2D>("Alphabet/im_p_cap"),
                ['Q'] = content.Load<Texture2D>("Alphabet/im_q_cap"),
                ['R'] = content.Load<Texture2D>("Alphabet/im_r_cap"),
                ['S'] = content.Load<Texture2D>("Alphabet/im_s_cap"),
                ['T'] = content.Load<Texture2D>("Alphabet/im_t_cap"),
                ['U'] = content.Load<Texture2D>("Alphabet/im_u_cap"),
                ['V'] = content.Load<Texture2D>("Alphabet/im_v_cap"),
                ['W'] = content.Load<Texture2D>("Alphabet/im_w_cap"),
                ['X'] = content.Load<Texture2D>("Alphabet/im_x_cap"),
                ['Y'] = content.Load<Texture2D>("Alphabet/im_y_cap"),
                ['Z'] = content.Load<Texture2D>("Alphabet/im_z_cap"),

                ['0'] = content.Load<Texture2D>("Alphabet/im_0"),
                ['1'] = content.Load<Texture2D>("Alphabet/im_1"),
                ['2'] = content.Load<Texture2D>("Alphabet/im_2"),
                ['3'] = content.Load<Texture2D>("Alphabet/im_3"),
                ['4'] = content.Load<Texture2D>("Alphabet/im_4"),
                ['5'] = content.Load<Texture2D>("Alphabet/im_5"),
                ['6'] = content.Load<Texture2D>("Alphabet/im_6"),
                ['7'] = content.Load<Texture2D>("Alphabet/im_7"),
                ['8'] = content.Load<Texture2D>("Alphabet/im_8"),
                ['9'] = content.Load<Texture2D>("Alphabet/im_9"),

                ['\''] = content.Load<Texture2D>("Alphabet/im__apostrophe"),
                ['('] = content.Load<Texture2D>("Alphabet/im__openbracket"),
                [')'] = content.Load<Texture2D>("Alphabet/im__closebracket"),
                [','] = content.Load<Texture2D>("Alphabet/im__comma"),
                ['.'] = content.Load<Texture2D>("Alphabet/im__dot"),
                ['+'] = content.Load<Texture2D>("Alphabet/im__plus"),
                ['-'] = content.Load<Texture2D>("Alphabet/im__dash"),
                ['/'] = content.Load<Texture2D>("Alphabet/im__slash"),
                ['?'] = content.Load<Texture2D>("Alphabet/im__question"),
                ['!'] = content.Load<Texture2D>("Alphabet/im__exclamation"),

                [':'] = content.Load<Texture2D>("Alphabet/im__colon"),
                //[';'] = content.Load<Texture2D>("Alphabet/im__semicolon"),

                [' '] = content.Load<Texture2D>("Alphabet/im__empty"),
            };
        }

        public Texture2D GetTexture(char letter) => this[letter];
        public Texture2D GetTexture(string letter) => this[letter];

        public Texture2D this[char letter] { get => this.TexturesByCharacter[letter]; }
        public Texture2D this[string letterContainer] { get => this[letterContainer[0]]; }

        #endregion

        #region Protected Methods
            
        protected void UpdateState()
        {
            // this.Lines = stateIndex switch
            // {
            //     _ => this.Lines = EMPTYTEXT
            // };
        }

        #endregion

    }
}
