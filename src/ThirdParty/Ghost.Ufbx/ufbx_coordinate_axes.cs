namespace Ghost.Ufbx;

public partial struct ufbx_coordinate_axes
{
    public static ufbx_coordinate_axes right_handed_y_up => new ufbx_coordinate_axes
    {
        right = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
        up = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Y,
        front = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Z,
    };
    public static ufbx_coordinate_axes right_handed_z_up => new ufbx_coordinate_axes
    {
        right = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
        up = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Z,
        front = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_NEGATIVE_Y,
    };
    public static ufbx_coordinate_axes left_handed_y_up => new ufbx_coordinate_axes
    {
        right = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
        up = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Y,
        front = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_NEGATIVE_Z,
    };
    public static ufbx_coordinate_axes left_handed_z_up => new ufbx_coordinate_axes
    {
        right = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_X,
        up = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Z,
        front = ufbx_coordinate_axis.UFBX_COORDINATE_AXIS_POSITIVE_Y,
    };
}