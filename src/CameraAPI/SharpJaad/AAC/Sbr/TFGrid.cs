namespace SharpJaad.AAC.Sbr
{
    public class TFGrid
    {
        /* function constructs new time border vector */
        /* first build into temp vector to be able to use previous vector on error */
        public static int EnvelopeTimeBorderVector(SBR sbr, int ch)
        {
            int l, border, temp;
            int[] t_E_temp = new int[6];

            t_E_temp[0] = sbr._rate * sbr._abs_bord_lead[ch];
            t_E_temp[sbr._L_E[ch]] = sbr._rate * sbr._abs_bord_trail[ch];

            switch (sbr._bs_frame_class[ch])
            {
                case Constants.FIXFIX:
                    switch (sbr._L_E[ch])
                    {
                        case 4:
                            temp = sbr._numTimeSlots / 4;
                            t_E_temp[3] = sbr._rate * 3 * temp;
                            t_E_temp[2] = sbr._rate * 2 * temp;
                            t_E_temp[1] = sbr._rate * temp;
                            break;
                        case 2:
                            t_E_temp[1] = sbr._rate * (sbr._numTimeSlots / 2);
                            break;
                        default:
                            break;
                    }
                    break;

                case Constants.FIXVAR:
                    if (sbr._L_E[ch] > 1)
                    {
                        int i = sbr._L_E[ch];
                        border = sbr._abs_bord_trail[ch];

                        for (l = 0; l < sbr._L_E[ch] - 1; l++)
                        {
                            if (border < sbr._bs_rel_bord[ch, l])
                                return 1;

                            border -= sbr._bs_rel_bord[ch, l];
                            t_E_temp[--i] = sbr._rate * border;
                        }
                    }
                    break;

                case Constants.VARFIX:
                    if (sbr._L_E[ch] > 1)
                    {
                        int i = 1;
                        border = sbr._abs_bord_lead[ch];

                        for (l = 0; l < sbr._L_E[ch] - 1; l++)
                        {
                            border += sbr._bs_rel_bord[ch, l];

                            if (sbr._rate * border + sbr._tHFAdj > sbr._numTimeSlotsRate + sbr._tHFGen)
                                return 1;

                            t_E_temp[i++] = sbr._rate * border;
                        }
                    }
                    break;

                case Constants.VARVAR:
                    if (sbr._bs_num_rel_0[ch] != 0)
                    {
                        int i = 1;
                        border = sbr._abs_bord_lead[ch];

                        for (l = 0; l < sbr._bs_num_rel_0[ch]; l++)
                        {
                            border += sbr._bs_rel_bord_0[ch, l];

                            if (sbr._rate * border + sbr._tHFAdj > sbr._numTimeSlotsRate + sbr._tHFGen)
                                return 1;

                            t_E_temp[i++] = sbr._rate * border;
                        }
                    }

                    if (sbr._bs_num_rel_1[ch] != 0)
                    {
                        int i = sbr._L_E[ch];
                        border = sbr._abs_bord_trail[ch];

                        for (l = 0; l < sbr._bs_num_rel_1[ch]; l++)
                        {
                            if (border < sbr._bs_rel_bord_1[ch, l])
                                return 1;

                            border -= sbr._bs_rel_bord_1[ch, l];
                            t_E_temp[--i] = sbr._rate * border;
                        }
                    }
                    break;
            }

            /* no error occured, we can safely use this t_E vector */
            for (l = 0; l < 6; l++)
            {
                sbr._t_E[ch, l] = t_E_temp[l];
            }

            return 0;
        }

        public static void NoiseFloorTimeBorderVector(SBR sbr, int ch)
        {
            sbr._t_Q[ch, 0] = sbr._t_E[ch, 0];

            if (sbr._L_E[ch] == 1)
            {
                sbr._t_Q[ch, 1] = sbr._t_E[ch, 1];
                sbr._t_Q[ch, 2] = 0;
            }
            else
            {
                int index = MiddleBorder(sbr, ch);
                sbr._t_Q[ch, 1] = sbr._t_E[ch, index];
                sbr._t_Q[ch, 2] = sbr._t_E[ch, sbr._L_E[ch]];
            }
        }

        private static int MiddleBorder(SBR sbr, int ch)
        {
            int retval = 0;

            switch (sbr._bs_frame_class[ch])
            {
                case Constants.FIXFIX:
                    retval = sbr._L_E[ch] / 2;
                    break;
                case Constants.VARFIX:
                    if (sbr._bs_pointer[ch] == 0)
                        retval = 1;
                    else if (sbr._bs_pointer[ch] == 1)
                        retval = sbr._L_E[ch] - 1;
                    else
                        retval = sbr._bs_pointer[ch] - 1;
                    break;
                case Constants.FIXVAR:
                case Constants.VARVAR:
                    if (sbr._bs_pointer[ch] > 1)
                        retval = sbr._L_E[ch] + 1 - sbr._bs_pointer[ch];
                    else
                        retval = sbr._L_E[ch] - 1;
                    break;
            }

            return retval > 0 ? retval : 0;
        }
    }
}
